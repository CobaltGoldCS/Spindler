namespace Spindler.Behaviors
{
    public class TextValidationBehavior : Behavior<Entry>
    {
        public Func<string, bool> validationFunction;
        public TextValidationBehavior(Func<string, bool> validationFunction)
        {
            this.validationFunction = validationFunction;
        }
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            base.OnDetachingFrom(entry);
        }

        void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            bool isValid = validationFunction.Invoke(args.NewTextValue);
            ((Entry)sender).TextColor = isValid ? Colors.Black : Colors.Red;
        }
    }
}
