namespace SuiviEntretien.Models;

public enum ApplicationStatus { Envoyee, Relance, Entretien, Offre, Refus, Cloturee }

public sealed class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime AppliedOn { get; set; } = DateTime.Today;
    public string Source { get; set; } = "LinkedIn";
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Envoyee;
    public string Recruiter { get; set; } = "";
    public string Contact { get; set; } = "";
    public DateTime? FollowUpOn { get; set; }
    public string Url { get; set; } = "";
    public string Notes { get; set; } = "";

    public string StatusText => Status switch
    {
        ApplicationStatus.Envoyee => "Envoyée",
        ApplicationStatus.Relance => "Relance",
        ApplicationStatus.Entretien => "Entretien",
        ApplicationStatus.Offre => "Offre",
        ApplicationStatus.Refus => "Refus",
        _ => "Clôturée"
    };
    public bool NeedsFollowUp => FollowUpOn is not null && FollowUpOn.Value.Date <= DateTime.Today
        && Status is not (ApplicationStatus.Refus or ApplicationStatus.Cloturee or ApplicationStatus.Offre);
}