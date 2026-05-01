using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class DeckValidationResult
{
    public List<string> errors = new List<string>();

    public bool IsValid
    {
        get { return errors.Count == 0; }
    }

    public void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            errors.Add(message);
        }
    }

    public string GetMessage()
    {
        if (IsValid)
        {
            return "Deck valid.";
        }

        StringBuilder builder = new StringBuilder();

        foreach (string error in errors)
        {
            builder.AppendLine("- " + error);
        }

        return builder.ToString();
    }
}
