﻿namespace Spindler.Behaviors;

public class TextValidationBehavior : Behavior<Entry>
{
    public Func<string, bool> validationFunction;
    private Color textColor = Colors.Black;
    private Color invalidTextColor;
    public TextValidationBehavior(Func<string, bool> validationFunction)
    {
        this.validationFunction = validationFunction;
        ResourceDictionary mergedDictionaries = Application.Current!.Resources;
        textColor = (Color)mergedDictionaries["TextColor"];
        invalidTextColor = (Color)mergedDictionaries["Warning"];
    }
    protected override void OnAttachedTo(Entry entry)
    {
        textColor = entry.TextColor;
        entry.TextChanged += OnEntryTextChanged;
        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnEntryTextChanged;
        base.OnDetachingFrom(entry);
    }

    void OnEntryTextChanged(object? sender, TextChangedEventArgs args)
    {
        bool isValid = validationFunction.Invoke(args.NewTextValue);
        ((Entry)sender!).TextColor = isValid ? textColor : invalidTextColor;
    }
}
