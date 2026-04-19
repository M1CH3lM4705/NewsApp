global using Application = Microsoft.Maui.Controls.Application;

namespace NewsApp.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new MainPage();
	}
}
