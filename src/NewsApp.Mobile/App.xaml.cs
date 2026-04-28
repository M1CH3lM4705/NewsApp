namespace NewsApp.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();

		try
		{
			MainPage = new MainPage();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"CRITICAL_ERROR: Falha ao inicializar MainPage: {ex.Message}");
			// Fallback para uma página vazia para evitar crash imediato
			MainPage = new ContentPage { Content = new VerticalStackLayout { Children = { new Label { Text = "Erro ao iniciar o aplicativo." } } } };
		}
	}
}
