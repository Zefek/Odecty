using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Playwright;

namespace Odecty.SMVAK;
internal class SMVAKWebForm
{
    public static async Task SubmitWaterMeterReadingAsync(WaterMeterReading r)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,           // DŮLEŽITÉ kvůli captcha
            SlowMo = 50                 // lidské tempo
        });

        var context = await browser.NewContextAsync(new()
        {
            Locale = "cs-CZ",
            ViewportSize = new() { Width = 1280, Height = 900 }
        });

        var page = await context.NewPageAsync();

        // 1️⃣ otevřít stránku
        await page.GotoAsync(
            "https://zis.smvak.cz/Forms/AF_Form019.aspx",
            new() { WaitUntil = WaitUntilState.NetworkIdle }
        );

        // 2️⃣ vyplnit formulář
        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_edDevice",
            r.DeviceNumber
        );

        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_edReadingDate",
            r.ReadingDate.ToString("yyyy-MM-dd")
        );

        await page.SelectOptionAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_ListReason",
            r.ReasonCode
        );

        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_edReadingValue",
            r.Value.ToString()
        );

        await page.SetInputFilesAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_afUpload",
            r.Filename
        );

        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_txtCustomerNumber",
            r.CustomerNumber
        );

        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_txtName",
            r.Name
        );

        await page.FillAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_EmailControl_edEmail",
            r.Email
        );

        // malá pauza – captcha + validace
        await page.WaitForTimeoutAsync(1500);

        // 3️⃣ odeslat
        await page.ClickAsync(
            "#ctl00_ctl00_ContentPlaceHolder1Common_ContentPlaceHolder1_btnSend"
        );

        // 4️⃣ počkat na výsledek
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000);

        // 5️⃣ jednoduchá kontrola úspěchu
        if (await page.Locator(".error:visible").CountAsync() > 0)
        {
            var errors = await page.Locator(".error:visible").AllInnerTextsAsync();
            throw new InvalidOperationException(
                "Odečet se nepodařilo odeslat:\n" + string.Join("\n", errors)
            );
        }

        // volitelně: screenshot pro log
        await page.ScreenshotAsync(new()
        {
            Path = $"odecet_{DateTime.Now:yyyyMMdd_HHmm}.png"
        });

        await browser.CloseAsync();
    }

}
