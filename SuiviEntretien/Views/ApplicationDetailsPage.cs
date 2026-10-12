using Microsoft.Maui.ApplicationModel;
using SuiviEntretien.Models;

namespace SuiviEntretien.Views;

public sealed class ApplicationDetailsPage : ContentPage
{
    private readonly JobApplication _application;

    public ApplicationDetailsPage(JobApplication application)
    {
        _application = application;
        Title = "Détails de l’offre";
        BackgroundColor = Color.FromArgb("#F4F7FB");

        var company = new Label { Text = application.Company, FontSize = 27, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#172338") };
        var role = new Label { Text = application.Role, FontSize = 17, TextColor = Color.FromArgb("#64748B") };
        var status = new Label { Text = application.StatusText, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#245FE5") };
        var details = new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                Field("Statut", application.StatusText),
                Field("Date de candidature", application.AppliedOn.ToString("dd/MM/yyyy")),
                Field("Site de candidature", application.Source),
                Field("Recruteur ou agence", application.Recruiter),
                Field("Téléphone ou e-mail", application.Contact),
                Field("Date de relance", application.FollowUpOn?.ToString("dd/MM/yyyy") ?? "Aucune"),
                Field("Notes pour l’appel", string.IsNullOrWhiteSpace(application.Notes) ? "Aucune note" : application.Notes)
            }
        };

        var url = application.Url?.Trim() ?? "";
        var linkLabel = new Label
        {
            Text = string.IsNullOrWhiteSpace(url) ? "Aucun lien enregistré" : url,
            FontSize = 13,
            TextColor = string.IsNullOrWhiteSpace(url) ? Color.FromArgb("#64748B") : Color.FromArgb("#245FE5")
        };
        var openLink = new Button
        {
            Text = "Consulter l’offre",
            IsEnabled = !string.IsNullOrWhiteSpace(url),
            BackgroundColor = Color.FromArgb("#245FE5"),
            TextColor = Colors.White,
            CornerRadius = 13,
            HeightRequest = 50,
            FontAttributes = FontAttributes.Bold
        };
        openLink.Clicked += async (_, _) => await OpenOfferAsync(url);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 15,
                Children =
                {
                    new Border
                    {
                        BackgroundColor = Colors.White,
                        Stroke = Color.FromArgb("#E3EAF3"),
                        StrokeShape = new RoundRectangle { CornerRadius = 16 },
                        Padding = 17,
                        Content = new VerticalStackLayout { Spacing = 5, Children = { company, role, status } }
                    },
                    new Border
                    {
                        BackgroundColor = Colors.White,
                        Stroke = Color.FromArgb("#E3EAF3"),
                        StrokeShape = new RoundRectangle { CornerRadius = 16 },
                        Padding = 17,
                        Content = details
                    },
                    new Border
                    {
                        BackgroundColor = Colors.White,
                        Stroke = Color.FromArgb("#E3EAF3"),
                        StrokeShape = new RoundRectangle { CornerRadius = 16 },
                        Padding = 17,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                new Label { Text = "Lien de l’annonce", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#172338") },
                                linkLabel,
                                openLink
                            }
                        }
                    }
                }
            }
        };
    }

    private static View Field(string title, string value) =>
        new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = title, FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                new Label { Text = string.IsNullOrWhiteSpace(value) ? "Non renseigné" : value, FontSize = 15, TextColor = Color.FromArgb("#172338") }
            }
        };

    private async Task OpenOfferAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await DisplayAlertAsync("Lien invalide", "Vérifie le lien de l’annonce dans la candidature.", "OK");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch
        {
            await DisplayAlertAsync("Impossible d’ouvrir le lien", "Vérifie que ton téléphone peut ouvrir les liens web.", "OK");
        }
    }
}