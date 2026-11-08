using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CloudQA_Task1;

class Program
{
    static void Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string url = config["AppSettings:BaseUrl"] ?? "";
        string firstName = config["AppSettings:FirstNameValue"] ?? "Sayali";
        string gender = config["AppSettings:GenderValue"] ?? "Female";
        string state = config["AppSettings:StateValue"] ?? "India";
        int waitSeconds = config.GetValue<int>("AppSettings:WaitSeconds", 20);

        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("BaseUrl is missing in appsettings.json.");
            return;
        }

        IWebDriver? driver = null;

        try
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            driver.Navigate().GoToUrl(url);

            WebDriverWait wait = new WebDriverWait(
                driver, TimeSpan.FromSeconds(waitSeconds));

            wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));

            Console.WriteLine("Page opened.");

            // Find and fill First Name
            IWebElement firstNameField = wait.Until(
                d => FindFirstName(d))!;

            firstNameField.Clear();
            firstNameField.SendKeys(firstName);

            if (firstNameField.GetAttribute("value") != firstName)
                throw new Exception("First Name verification failed.");

            Console.WriteLine("First Name: PASS");

            // Find and select Gender
            IWebElement genderRadio = wait.Until(
                d => FindGender(d, gender))!;

            try
            {
                ScrollToElement(driver, genderRadio);
                genderRadio.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].click();", genderRadio);
            }
            catch (ElementNotInteractableException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(
                    "arguments[0].click();", genderRadio);
            }

            if (!genderRadio.Selected)
                throw new Exception("Gender verification failed.");

            Console.WriteLine("Gender: PASS");

            // Find and select State
            IWebElement stateDropdown = wait.Until(
                d => FindStateDropdown(d, state))!;

            ScrollToElement(driver, stateDropdown);
            SelectState(stateDropdown, state);

            Console.WriteLine("State: PASS");

            Console.WriteLine();
            Console.WriteLine("All three field checks completed.");
            Console.WriteLine("Browser is open.");
            Console.WriteLine("Press Enter to close the browser.");

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("TEST FAILED");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Press Enter to close the browser.");
            Console.ReadLine();
        }
        finally
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
    }

    // Find First Name
    static IWebElement FindFirstName(IWebDriver driver)
    {
        List<IWebElement> fields = driver.FindElements(
            By.TagName("input"))
            .Where(e => IsUsable(e) &&
                (Normalize(e.GetAttribute("type")) == "text" ||
                 string.IsNullOrWhiteSpace(e.GetAttribute("type"))))
            .ToList();

        List<(IWebElement element, int score)> matches = new();

        foreach (IWebElement field in fields)
        {
            string placeholder = Normalize(field.GetAttribute("placeholder"));
            string aria = Normalize(field.GetAttribute("aria-label"));
            string label = Normalize(GetAssociatedLabelText(field));
            string parent = Normalize(GetNearbyText(field));

            int score = 0;

            if (placeholder.Contains("first name")) score += 100;
            if (aria.Contains("first name")) score += 100;
            if (label.Contains("first name")) score += 100;
            if (parent.Contains("first name")) score += 60;

            if (placeholder == "name") score += 15;
            if (aria == "name") score += 15;

            if (placeholder.Contains("last") ||
                aria.Contains("last") ||
                label.Contains("last name") ||
                parent.Contains("last name") ||
                placeholder.Contains("surname") ||
                label.Contains("surname"))
            {
                score -= 100;
            }

            if (score > 0)
                matches.Add((field, score));
        }

        return GetUniqueBestMatch(matches, "First Name");
    }

    // Find Gender radio button
    static IWebElement FindGender(IWebDriver driver, string wanted)
    {
        List<IWebElement> radios = driver.FindElements(
            By.XPath("//input[@type='radio']"))
            .Where(e =>
            {
                try
                {
                    return e.Enabled;
                }
                catch
                {
                    return false;
                }
            })
            .ToList();

        List<(IWebElement element, int score, int index)> matches = new();

        for (int i = 0; i < radios.Count; i++)
        {
            IWebElement radio = radios[i];

            string label = Normalize(GetAssociatedLabelText(radio));
            string value = Normalize(radio.GetAttribute("value"));
            string aria = Normalize(radio.GetAttribute("aria-label"));
            string group = Normalize(GetRadioGroupContext(radio));

            bool optionMatches =
                label == Normalize(wanted) ||
                value == Normalize(wanted) ||
                aria == Normalize(wanted);

            if (!optionMatches)
                continue;

            int score = 0;

            if (group.Contains("gender"))
                score += 1000;

            if (group.Contains("sex"))
                score += 900;

            if (label == Normalize(wanted))
                score += 100;

            if (aria == Normalize(wanted))
                score += 80;

            if (value == Normalize(wanted))
                score += 60;

            matches.Add((radio, score, i));
        }

        if (matches.Count == 0)
        {
            throw new Exception(
                "Gender option '" + wanted + "' was not found.");
        }

        var best = matches
            .OrderByDescending(m => m.score)
            .ThenBy(m => m.index)
            .First();

        return best.element;
    }

    // Find State dropdown
    static IWebElement FindStateDropdown(
        IWebDriver driver, string wanted)
    {
        List<IWebElement> dropdowns = driver.FindElements(
            By.TagName("select"))
            .Where(IsUsable)
            .ToList();

        List<(IWebElement element, int score)> matches = new();

        foreach (IWebElement dropdown in dropdowns)
        {
            List<IWebElement> options = dropdown.FindElements(
                By.TagName("option")).ToList();

            bool optionExists = options.Any(option =>
                SameText(option.Text, wanted) ||
                SameText(option.GetAttribute("value"), wanted));

            if (!optionExists)
                continue;

            string label = Normalize(GetAssociatedLabelText(dropdown));
            string parent = Normalize(GetNearbyText(dropdown));
            string aria = Normalize(dropdown.GetAttribute("aria-label"));

            int score = 0;

            if (label.Contains("state")) score += 100;
            if (aria.Contains("state")) score += 100;
            if (parent.Contains("state")) score += 60;

            if (label.Contains("country")) score += 90;
            if (aria.Contains("country")) score += 90;
            if (parent.Contains("country")) score += 50;

            if (score > 0)
                matches.Add((dropdown, score));
        }

        if (matches.Count == 0)
        {
            List<IWebElement> possible = dropdowns
                .Where(dropdown => dropdown.FindElements(By.TagName("option"))
                    .Any(option =>
                        SameText(option.Text, wanted) ||
                        SameText(option.GetAttribute("value"), wanted)))
                .ToList();

            if (possible.Count == 1)
                return possible[0];
        }

        return GetUniqueBestMatch(matches, "State dropdown");
    }

    // Select State
    static void SelectState(IWebElement dropdown, string wanted)
    {
        SelectElement select = new SelectElement(dropdown);
        bool found = false;

        foreach (IWebElement option in select.Options)
        {
            if (SameText(option.Text, wanted))
            {
                select.SelectByText(option.Text);
                found = true;
                break;
            }

            string optionValue = option.GetAttribute("value") ?? "";

            if (SameText(optionValue, wanted))
            {
                if (optionValue.Length > 0)
                    select.SelectByValue(optionValue);
                else
                    select.SelectByText(option.Text);

                found = true;
                break;
            }
        }

        if (!found)
        {
            string available = string.Join(", ",
                select.Options.Select(o => o.Text.Trim())
                    .Where(t => t.Length > 0));

            throw new Exception(
                "Option '" + wanted + "' was not found. Available options: " +
                available);
        }

        if (!SameText(select.SelectedOption.Text, wanted) &&
            !SameText(select.SelectedOption.GetAttribute("value"), wanted))
        {
            throw new Exception("State selection could not be verified.");
        }
    }

    // Get associated label
    static string GetAssociatedLabelText(IWebElement element)
    {
        try
        {
            IWebDriver driver = ((IWrapsDriver)element).WrappedDriver;
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            object? result = js.ExecuteScript(@"
                const e = arguments[0];
                let texts = [];

                if (e.labels) {
                    for (const label of e.labels) {
                        texts.push(label.innerText || label.textContent || '');
                    }
                }

                const wrappingLabel = e.closest('label');

                if (wrappingLabel) {
                    texts.push(wrappingLabel.innerText ||
                               wrappingLabel.textContent || '');
                }

                return texts.join(' ');
            ", element);

            return Convert.ToString(result) ?? "";
        }
        catch
        {
            return "";
        }
    }

    // Get nearby text
    static string GetNearbyText(IWebElement element)
    {
        try
        {
            IWebDriver driver = ((IWrapsDriver)element).WrappedDriver;
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            object? result = js.ExecuteScript(@"
                const e = arguments[0];
                let texts = [];
                let p = e.parentElement;

                for (let i = 0; i < 3 && p; i++) {
                    const text = p.innerText || p.textContent || '';
                    if (text.trim()) texts.push(text);
                    p = p.parentElement;
                }

                return texts.join(' ');
            ", element);

            return Convert.ToString(result) ?? "";
        }
        catch
        {
            return "";
        }
    }

    // Get radio group text
    static string GetRadioGroupContext(IWebElement radio)
    {
        try
        {
            IWebDriver driver = ((IWrapsDriver)radio).WrappedDriver;
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            object? result = js.ExecuteScript(@"
                const e = arguments[0];
                const group = e.closest('fieldset, [role=""radiogroup""]');

                if (group) {
                    const legend = group.querySelector('legend');

                    return [
                        group.getAttribute('aria-label') || '',
                        legend ? (legend.innerText || legend.textContent) : ''
                    ].join(' ');
                }

                let p = e.parentElement;

                for (let i = 0; i < 5 && p; i++) {
                    const text = p.innerText || p.textContent || '';

                    if (/gender|sex/i.test(text) && text.length < 250)
                        return text;

                    p = p.parentElement;
                }

                return '';
            ", radio);

            return Convert.ToString(result) ?? "";
        }
        catch
        {
            return "";
        }
    }

    // Find the best matching element
    static IWebElement GetUniqueBestMatch(
        List<(IWebElement element, int score)> matches,
        string fieldName)
    {
        if (matches.Count == 0)
            throw new Exception(fieldName + " could not be identified.");

        int highestScore = matches.Max(m => m.score);

        List<IWebElement> best = matches
            .Where(m => m.score == highestScore)
            .Select(m => m.element)
            .ToList();

        if (best.Count != 1)
            throw new Exception(
                "More than one possible " + fieldName +
                " field was found. Selection stopped to avoid choosing the wrong field.");

        return best[0];
    }

    // Check element
    static bool IsUsable(IWebElement element)
    {
        try
        {
            return element.Displayed && element.Enabled;
        }
        catch
        {
            return false;
        }
    }

    // Scroll to element
    static void ScrollToElement(IWebDriver driver, IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'center'});", element);
    }

    // Clean text
    static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return Regex.Replace(
            value.Trim(), @"\s+", " ").ToLowerInvariant();
    }

    // Compare text
    static bool SameText(string? actual, string? expected)
    {
        return Normalize(actual) == Normalize(expected);
    }
}