namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
[QueryProperty(nameof(Message), "errormessage")]
public partial class ErrorPage : ContentPage
{

	public string? BookId { private get; set; } = null;
    public string Message 
	{ 
		set
		{
			LoadPage(value);
		}
	}
	private void LoadPage(string message)
	{
        ErrorLabel.Text = message;
    }
    public ErrorPage()
	{
		InitializeComponent();
	}

	private async void WebviewButton_Clicked(object sender, EventArgs e)
	{
		if (!ValidId()) return;
		await Shell.Current.GoToAsync($"../{nameof(WebviewReaderPage)}?id={BookId}");
    }

	private bool ValidId() => BookId != null;
}