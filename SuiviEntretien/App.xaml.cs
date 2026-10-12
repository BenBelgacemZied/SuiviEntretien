using SuiviEntretien.Views;

namespace SuiviEntretien;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#F4F7FB"),
            BarTextColor = Color.FromArgb("#172338")
        };
    }
}