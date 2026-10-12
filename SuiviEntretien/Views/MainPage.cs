using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using SuiviEntretien.Models;
using SuiviEntretien.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SuiviEntretien.Views;

public sealed class MainPage : ContentPage
{
    private readonly ApplicationStore _store;
    private readonly ObservableCollection<JobApplication> _items = new();
    private readonly CollectionView _list;
    private readonly SearchBar _search;
    private readonly Picker _filter;
    private readonly Label _summary;

    public MainPage(ApplicationStore store)
    {
        _store = store;
        Title = "Mes candidatures";
        BackgroundColor = Color.FromArgb("#F4F7FB");

        _summary = new Label { FontSize = 13, TextColor = Color.FromArgb("#64748B") };
        _search = new SearchBar { Placeholder = "Entreprise, poste, recruteur…", BackgroundColor = Colors.White };
        _search.TextChanged += (_, _) => Refresh();
        _filter = new Picker { Title = "Tous les statuts", ItemsSource = new[] { "Tous", "Envoyée", "Relance", "Entretien", "Offre", "Refus", "Clôturée" }, SelectedIndex = 0, BackgroundColor = Colors.White };
        _filter.SelectedIndexChanged += (_, _) => Refresh();
        _list = new CollectionView { ItemsSource = _items, SelectionMode = SelectionMode.Single, ItemTemplate = new DataTemplate(CreateCard) };
        _list.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is JobApplication selected)
                await Navigation.PushAsync(new ApplicationDetailsPage(selected));
            _list.SelectedItem = null;
        };

        var add = new Button { Text = "+  Ajouter une candidature", BackgroundColor = Color.FromArgb("#245FE5"), TextColor = Colors.White, CornerRadius = 13, HeightRequest = 52, FontAttributes = FontAttributes.Bold };
        add.Clicked += async (_, _) => await EditAsync(null);
        var exportButton = new Button { Text = "Exporter la sauvegarde", FontSize = 12, BackgroundColor = Colors.White, TextColor = Color.FromArgb("#245FE5"), CornerRadius = 11 };
        exportButton.Clicked += async (_, _) => await ExportBackupAsync();
        var importButton = new Button { Text = "Restaurer une sauvegarde", FontSize = 12, BackgroundColor = Colors.White, TextColor = Color.FromArgb("#245FE5"), CornerRadius = 11 };
        importButton.Clicked += async (_, _) => await ImportBackupAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 16),
                Spacing = 12,
                Children =
                {
                    new Label { Text = "RECHERCHE D’EMPLOI", TextColor = Color.FromArgb("#245FE5"), FontSize = 12, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Mes candidatures", FontSize = 29, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#172338") },
                    new Label { Text = "Retrouve vite l’offre quand un recruteur t’appelle.", FontSize = 14, TextColor = Color.FromArgb("#64748B") },
                    _summary,
                    new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E3EAF3"), StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 14, Content = new Label { Text = "Après avoir postulé, entreprise et poste suffisent. Tu pourras compléter les détails plus tard.", FontSize = 13, TextColor = Color.FromArgb("#46546A") } },
                    _search,
                    _filter,
                    add,
                    new HorizontalStackLayout { Spacing = 8, Children = { exportButton, importButton } },
                    _list
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _items.Clear();
        foreach (var item in (await _store.GetAllAsync()).OrderByDescending(x => x.AppliedOn)) _items.Add(item);
        Refresh();
    }

    private View CreateCard()
    {
        var company = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#172338") };
        company.SetBinding(Label.TextProperty, nameof(JobApplication.Company));
        var role = new Label { FontSize = 14, TextColor = Color.FromArgb("#64748B") };
        role.SetBinding(Label.TextProperty, nameof(JobApplication.Role));
        var status = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#245FE5") };
        status.SetBinding(Label.TextProperty, nameof(JobApplication.StatusText));
        var info = new Label { FontSize = 12, TextColor = Color.FromArgb("#64748B") };
        info.BindingContextChanged += (_, _) => { if (info.BindingContext is JobApplication a) info.Text = $"{a.AppliedOn:dd/MM/yyyy} · {a.Source}" + (a.FollowUpOn is null ? "" : $" · Relance {a.FollowUpOn:dd/MM}"); };
        var contact = new Label { FontSize = 13, TextColor = Color.FromArgb("#46546A") };
        contact.BindingContextChanged += (_, _) => { if (contact.BindingContext is JobApplication a) contact.Text = string.Join(" · ", new[] { a.Recruiter, a.Contact }.Where(x => !string.IsNullOrWhiteSpace(x))); };
        var notes = new Label { FontSize = 13, TextColor = Color.FromArgb("#46546A"), MaxLines = 3 };
        notes.SetBinding(Label.TextProperty, nameof(JobApplication.Notes));
        var edit = new Button { Text = "Modifier", FontSize = 12, Padding = new Thickness(12, 6), BackgroundColor = Color.FromArgb("#F0F4FA"), TextColor = Color.FromArgb("#172338") };
        edit.Clicked += async (_, _) => { if (edit.BindingContext is JobApplication a) await EditAsync(a); };
        var delete = new Button { Text = "Supprimer", FontSize = 12, Padding = new Thickness(12, 6), BackgroundColor = Color.FromArgb("#FCEBED"), TextColor = Color.FromArgb("#B42335") };
        delete.Clicked += async (_, _) =>
        {
            if (delete.BindingContext is JobApplication a && await DisplayAlert("Supprimer", $"Supprimer {a.Company} ?", "Supprimer", "Annuler"))
            { await _store.DeleteAsync(a.Id); _items.Remove(a); Refresh(); }
        };
        return new Border
        {
            BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E3EAF3"), StrokeShape = new RoundRectangle { CornerRadius = 15 }, Padding = 14, Margin = new Thickness(0, 0, 0, 9),
            Content = new VerticalStackLayout { Spacing = 5, Children = { company, role, status, info, contact, notes, new HorizontalStackLayout { HorizontalOptions = LayoutOptions.End, Children = { edit, delete } } } }
        };
    }

    private void Refresh()
    {
        var query = _search.Text?.Trim() ?? "";
        var filter = _filter.SelectedItem?.ToString() ?? "Tous";
        var matches = _items.Where(a => (filter == "Tous" || a.StatusText == filter) &&
            new[] { a.Company, a.Role, a.Recruiter, a.Contact, a.Source, a.Notes }.Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        _list.ItemsSource = matches;
        _summary.Text = $"{_items.Count} candidature(s) · {_items.Count(x => x.Status is ApplicationStatus.Envoyee or ApplicationStatus.Relance)} en attente · {_items.Count(x => x.Status == ApplicationStatus.Entretien)} entretien(s) · {_items.Count(x => x.NeedsFollowUp)} à relancer";
    }


    private async Task ExportBackupAsync()
    {
        try
        {
            var data = await _store.GetAllAsync();
            var path = Path.Combine(FileSystem.CacheDirectory, $"suivientretien-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Sauvegarder mes candidatures",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export impossible", ex.Message, "OK");
        }
    }

    private async Task ImportBackupAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choisir une sauvegarde SuiviEntretien" });
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            var imported = await JsonSerializer.DeserializeAsync<List<JobApplication>>(stream);
            if (imported is null || imported.Any(x => string.IsNullOrWhiteSpace(x.Company) || string.IsNullOrWhiteSpace(x.Role)))
            {
                await DisplayAlertAsync("Fichier invalide", "Choisis un fichier de sauvegarde SuiviEntretien valide.", "OK");
                return;
            }

            var confirm = await DisplayAlertAsync("Restaurer une sauvegarde",
                $"Cette action remplacera les {_items.Count} candidature(s) actuelles par les {imported.Count} candidature(s) de la sauvegarde.",
                "Restaurer", "Annuler");
            if (!confirm) return;

            await _store.ReplaceAllAsync(imported);
            _items.Clear();
            foreach (var item in imported.OrderByDescending(x => x.AppliedOn)) _items.Add(item);
            Refresh();
            await DisplayAlertAsync("Sauvegarde restaurée", $"{imported.Count} candidature(s) restaurée(s).", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Restauration impossible", ex.Message, "OK");
        }
    }

    private async Task EditAsync(JobApplication? existing)
    {
        var item = existing is null ? new JobApplication() : new JobApplication
        {
            Id = existing.Id, Company = existing.Company, Role = existing.Role, AppliedOn = existing.AppliedOn, Source = existing.Source,
            Status = existing.Status, Recruiter = existing.Recruiter, Contact = existing.Contact, FollowUpOn = existing.FollowUpOn, Url = existing.Url, Notes = existing.Notes
        };
        var company = new Entry { Placeholder = "Entreprise *", Text = item.Company };
        var role = new Entry { Placeholder = "Intitulé du poste *", Text = item.Role };
        var source = new Picker { Title = "Site / source", ItemsSource = new[] { "LinkedIn", "Indeed", "ICTjob", "Site du recruteur", "Autre" }, SelectedItem = item.Source };
        var status = new Picker { Title = "Statut", ItemsSource = new[] { "Envoyée", "Relance", "Entretien", "Offre", "Refus", "Clôturée" }, SelectedItem = item.StatusText };
        var recruiter = new Entry { Placeholder = "Recruteur ou agence", Text = item.Recruiter };
        var contact = new Entry { Placeholder = "Téléphone ou e-mail", Text = item.Contact };
        var url = new Entry { Placeholder = "Lien de l’annonce", Text = item.Url, Keyboard = Keyboard.Url };
        var follow = new DatePicker { Date = item.FollowUpOn ?? DateTime.Today, Format = "dd/MM/yyyy" };
        var notes = new Editor { Placeholder = "Notes utiles pour l’appel", Text = item.Notes, AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 90 };
        var form = new VerticalStackLayout { Padding = 18, Spacing = 10, Children = { company, role, source, status, recruiter, contact, url, new Label { Text = "Date de relance (laisser aujourd’hui si inutile)", FontSize = 12 }, follow, notes } };
        var page = new ContentPage { Title = existing is null ? "Nouvelle candidature" : "Modifier", BackgroundColor = Color.FromArgb("#F4F7FB"), Content = new ScrollView { Content = form } };
        var save = new ToolbarItem { Text = "Enregistrer" };
        save.Clicked += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(company.Text) || string.IsNullOrWhiteSpace(role.Text))
            { await page.DisplayAlert("Informations manquantes", "Indique l’entreprise et le poste.", "OK"); return; }
            item.Company = company.Text.Trim(); item.Role = role.Text.Trim(); item.Source = source.SelectedItem?.ToString() ?? "Autre";
            item.Status = status.SelectedItem?.ToString() switch { "Relance" => ApplicationStatus.Relance, "Entretien" => ApplicationStatus.Entretien, "Offre" => ApplicationStatus.Offre, "Refus" => ApplicationStatus.Refus, "Clôturée" => ApplicationStatus.Cloturee, _ => ApplicationStatus.Envoyee };
            item.Recruiter = recruiter.Text?.Trim() ?? ""; item.Contact = contact.Text?.Trim() ?? ""; item.Url = url.Text?.Trim() ?? "";
            item.FollowUpOn = follow.Date == DateTime.Today ? null : follow.Date; item.Notes = notes.Text?.Trim() ?? "";
            await _store.SaveAsync(item); var old = _items.FirstOrDefault(x => x.Id == item.Id); if (old is not null) _items.Remove(old); _items.Insert(0, item); Refresh(); await Navigation.PopAsync();
        };
        page.ToolbarItems.Add(save);
        await Navigation.PushAsync(page);
    }
}