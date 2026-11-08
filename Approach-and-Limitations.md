# Approach and Limitations

## 1. Approach

While building this project, my main focus was to avoid depending only on fixed element IDs, names, or CSS classes.

I used C# with Selenium WebDriver to automate the three required fields on the CloudQA practice form.

### First Name

The program checks different details such as the placeholder, accessible label, associated label, and nearby text to identify the First Name textbox.

It also checks for clues that may indicate a different field, such as Last Name or Surname.

### Gender

The program searches for radio buttons and checks their labels and values. It also considers the surrounding radio group context before choosing the requested option.

### State

The program checks the available dropdown elements and their options. It also uses nearby labels and page context to identify the appropriate dropdown.

### Waiting and Verification

I used explicit waits so the program does not try to interact with elements before they are ready.

After each action, the program checks the result and prints a PASS message when the requested interaction is successful.

## 2. Handling Page Changes

The program uses multiple clues instead of relying on one fixed selector.

This can help when some element attributes change or when the order of fields is different.

However, these changes need separate testing to confirm that the automation continues to work correctly.

## 3. Limitations

There are some situations where the current implementation may need improvement:

- **Similar Fields:** If multiple fields have very similar labels, placeholders, or values, the program may not always identify the intended field correctly.
- **Major HTML Changes:** If the page removes useful labels, accessible names, and nearby text, identifying the correct field becomes difficult.
- **Iframes and Shadow DOM:** The current implementation searches the main page document. It does not automatically handle every iframe or Shadow DOM element.
- **Dropdown Options:** The configured State value must exist in the dropdown. If the available options change, the configuration may need to be updated.
- **Page-Specific Logic:** The current matching approach is designed around the provided practice form and is not guaranteed to work on every website.

## 4. Current Testing Status

The program was run successfully on the provided CloudQA practice form.

The following checks passed:

- First Name textbox interaction.
- Gender radio button selection.
- State dropdown selection.

Testing with reordered fields, changed IDs/names/classes, and multiple similar-looking fields still needs to be performed separately.

## 5. Conclusion

The project demonstrates a more flexible way of locating form elements with Selenium. Instead of depending only on fixed selectors, it uses labels, attributes, and surrounding page information.

There is still room to improve the matching logic, especially when a page contains multiple similar fields or undergoes major structural changes.