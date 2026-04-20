using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace CEZPlyn;
class OdecetCezPlyn
{
    public static async Task SubmitGasMeterReadingAsync()
    {

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
        });
        var context = await browser.NewContextAsync();

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://www.cez.cz/");
        var page1 = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.GetByRole(AriaRole.Link, new() { Name = "Můj ČEZ", Exact = true }).ClickAsync();
        });
        await page1.Locator("#iframeCookies").ContentFrame.GetByRole(AriaRole.Button, new() { Name = "Allow all" }).ClickAsync();
        await page1.Locator("#username").ClickAsync();
        await page1.Locator("#username").FillAsync("");
        await page1.Locator("#username").PressAsync("Tab");
        await page1.Locator("#password").FillAsync("");
        await page1.GetByRole(AriaRole.Button, new() { Name = "Přihlásit se" }).ClickAsync();
        await page1.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button, new() { Name = "Plyn" }).ClickAsync();
        await page1.GetByRole(AriaRole.Button, new() { Name = "" }).ClickAsync();
        await page1.GetByRole(AriaRole.Button, new() { Name = "Sledování spotřeby" }).ClickAsync();
        await page1.GetByRole(AriaRole.Link, new() { Name = "Zadat samoodečet" }).ClickAsync();
        await page1.GetByRole(AriaRole.Button, new() { Name = "Kontrolní odečet pro" }).ClickAsync();
        await page1.Locator("form[name=\"selfConsumptionMeasurement\"]").GetByRole(AriaRole.Button).Filter(new() { HasTextRegex = new Regex("^$") }).ClickAsync();
        await page1.GetByRole(AriaRole.Gridcell, new() { Name = "1", Exact = true }).ClickAsync();
        await page1.GetByRole(AriaRole.Textbox, new() { Name = "Stav plynoměru *" }).ClickAsync();
        await page1.GetByRole(AriaRole.Textbox, new() { Name = "Stav plynoměru *" }).FillAsync("0");
        await page1.GetByRole(AriaRole.Button, new() { Name = "Uložit" }).ClickAsync();
    }
}