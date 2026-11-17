using System.Globalization;
using System.Text;
using DocFlowHub.Core.Constants;

namespace DocFlowHub.Infrastructure.Data.Demo;

/// <summary>A version of a demo document as the text templates see it.</summary>
public sealed record DemoVersionInfo(int Number, string Author, DateTime Date, string Summary);

/// <summary>
/// The text of the demo documents (Sprint 24). Every kind of document has its own structure (headings,
/// lists, tables, code) filled with facts derived from the title, and every version changes specific lines,
/// named by <see cref="ChangeSummary"/>, so the line diff and the AI comparisons have real edits to show.
/// Everything is a pure function of the catalogue entry, the version and its history: a file can be rebuilt.
/// </summary>
public static class DemoContent
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ------------------------------------------------------------------ change summaries

    private static readonly Dictionary<DemoKind, string[]> Changes = new()
    {
        [DemoKind.MeetingNotes] = new[] { "Added the action items agreed after the meeting", "Marked the first action item as done", "Corrected the attendee list", "Added the date of the follow-up", "Clarified the decision on scope", "Final notes after review" },
        [DemoKind.Contract] = new[] { "Updated the monthly fee after the vendor call", "Added clause 7: data retention", "Extended the term to 24 months", "Removed the early-termination fee", "Legal review: clarified the liability cap", "Signed by both parties" },
        [DemoKind.Spec] = new[] { "Added a goal for offline use", "Added the batch endpoint to the API table", "Resolved the open question on rate limits", "Added the rollout plan", "Approved by the product team", "Added the success metrics" },
        [DemoKind.ReleaseNotes] = new[] { "Added the hotfix for the login loop", "Clarified the upgrade steps" },
        [DemoKind.Budget] = new[] { "Cut travel by 10% after the first review", "Added a line for tools and licences", "Raised people costs for two new hires", "Final figures from Finance", "Approved by the CFO", "Moved part of Q4 spend to Q1" },
        [DemoKind.Policy] = new[] { "Added contractors to the scope", "Raised the daily meal allowance", "Added a section on exceptions", "Approved by the leadership team", "Updated the contact for questions", "Yearly review; no change to the rules" },
        [DemoKind.Postmortem] = new[] { "Corrected the start time in the timeline", "Added the root cause after the investigation", "Marked two action items as done", "Added the lessons learned", "Final review with the team", "Linked the follow-up tickets" },
        [DemoKind.Guide] = new[] { "Added a step to check the backups first", "Updated the command for the new CLI", "Added a troubleshooting section", "Reviewed after the last drill", "Added the escalation contacts", "Shortened the checklist" },
        [DemoKind.Report] = new[] { "Updated the figures with the final numbers", "Added commentary on the churn spike", "Corrected the headcount figure", "Added the next steps", "Reviewed by Finance", "Final version for the board" },
        [DemoKind.Proposal] = new[] { "Narrowed the scope after the kick-off", "Moved the launch date by two weeks", "Added the budget estimate", "Added a phase for user testing", "Approved by the steering group", "Updated the owners of each phase" },
        [DemoKind.Invoice] = new[] { "Corrected the VAT rate", "Added the purchase order number" },
        [DemoKind.LogExport] = new[] { "Added the entries from the following day", "Removed personal data from the export" },
        [DemoKind.Research] = new[] { "Added two more interviews", "Grouped the findings by theme", "Added the recommendations", "Reviewed with the product team" },
        [DemoKind.Press] = new[] { "Added a quote from the CEO", "Updated the availability date", "Approved for release" },
        [DemoKind.JobDescription] = new[] { "Raised the salary range", "Removed the degree requirement" },
        [DemoKind.BarChart] = new[] { "Updated with the final figures", "Added the latest period", "Fixed the colours of the series" },
        [DemoKind.LineChart] = new[] { "Added the latest month", "Corrected a data error in March", "Added a second series" },
        [DemoKind.Wireframe] = new[] { "Added a call-to-action button to the hero", "Split the hero into text and image", "Added more cards to the feature grid", "Spacing fixes after the design review" },
        [DemoKind.Diagram] = new[] { "Added the notification service", "Added the search service", "Renamed the gateway" },
        [DemoKind.Donut] = new[] { "Updated the split after the budget review", "Merged two small teams into Other" },
    };

    /// <summary>What version <paramref name="version"/> (2 or later) changed.</summary>
    public static string ChangeSummary(DemoDoc d, int version)
    {
        if (version <= 1) return d.Kind == DemoKind.Invoice ? "Invoice received" : "First version";
        var list = Changes[d.Kind];
        if (d.Kind == DemoKind.Policy && version == 3 && !IsTravel(d)) return "Shortened the approval time to three working days";
        if (d.Kind == DemoKind.Report && version == 4 && ReportKind(d) != "kpi") return "Corrected a figure in the main table";
        if (d.Kind == DemoKind.Report && version == 3 && ReportKind(d) != "kpi") return "Added commentary on the main change";
        return version - 2 < list.Length ? list[version - 2] : $"Revision {version}";
    }

    /// <summary>The changes between two versions, in order, as the AI comparison lists them.</summary>
    public static IReadOnlyList<string> ChangesBetween(DemoDoc d, int from, int to) =>
        Enumerable.Range(from + 1, Math.Max(0, to - from)).Select(v => ChangeSummary(d, v)).ToList();

    private static bool IsTravel(DemoDoc d) => d.Title.Contains("Travel", StringComparison.Ordinal);

    /// <summary>Which layout a report uses, from its title.</summary>
    private static string ReportKind(DemoDoc d) =>
        d.Title.StartsWith("Findings", StringComparison.Ordinal) || d.Title.StartsWith("Penetration", StringComparison.Ordinal) ? "findings"
        : d.Title.StartsWith("Error budget", StringComparison.Ordinal) ? "slo"
        : d.Title.StartsWith("Hiring", StringComparison.Ordinal) || d.Title.StartsWith("Headcount", StringComparison.Ordinal) ? "hiring"
        : d.Title.StartsWith("Onboarding survey", StringComparison.Ordinal) ? "survey"
        : d.Title.StartsWith("All-hands", StringComparison.Ordinal) ? "agenda"
        : "kpi";

    /// <summary>Runbooks and engineering guides get commands; the others are business checklists.</summary>
    private static bool IsOps(DemoDoc d) =>
        d.Title.StartsWith("Runbook", StringComparison.Ordinal) || d.Title.StartsWith("On-call", StringComparison.Ordinal) || d.Title.StartsWith("QA", StringComparison.Ordinal);

    // ------------------------------------------------------------------ facts from the title

    /// <summary>Stable facts for a document: the same title always gives the same numbers and names.</summary>
    private sealed class Facts
    {
        private readonly uint _hash;

        public Facts(string title)
        {
            var hash = 2166136261u;   // FNV-1a: string.GetHashCode changes between runs
            foreach (var b in Encoding.UTF8.GetBytes(title))
            {
                hash = (hash ^ b) * 16777619u;
            }
            _hash = hash;
        }

        public int Seed => (int)(_hash & 0x7FFFFFFF);

        public int N(int salt, int min, int max) => min + (int)((_hash * 2654435761u ^ (uint)(salt * 40503)) % (uint)(max - min + 1));

        public string Pick(int salt, params string[] options) => options[N(salt, 0, options.Length - 1)];
    }

    public static int PictureSeed(DemoDoc d) => new Facts(d.Title).Seed;

    private static string Money(decimal value) => "$" + value.ToString("N0", Inv);
    private static string Day(DateTime date) => date.ToString("d MMM yyyy", Inv);

    /// <summary>The people a document's text names: the owner first, then the team.</summary>
    public static IReadOnlyList<string> People(DemoDoc d)
    {
        var keys = new List<string> { d.Owner };
        var team = DemoCatalog.Teams.FirstOrDefault(t => t.Name == d.Team);
        if (team != null) keys.AddRange(team.Members.Select(m => m.Person).Where(k => k != d.Owner));
        foreach (var k in new[] { "maya", "tom", "sam" }) if (!keys.Contains(k)) keys.Add(k);
        return keys.Select(NameOf).ToList();
    }

    public static string NameOf(string key) => key switch
    {
        DemoCatalog.User => $"{DemoAccounts.UserFirstName} {DemoAccounts.UserLastName}",
        DemoCatalog.Admin => $"{DemoAccounts.AdminFirstName} {DemoAccounts.AdminLastName}",
        _ => DemoCatalog.Person(key)?.Name ?? key,
    };

    // ------------------------------------------------------------------ the line builder

    private sealed class Text
    {
        public readonly List<string> Lines = new();
        public Text H(string heading) { Lines.Add(DemoFileFactory.HeadingPrefix + heading); return this; }
        public Text P(string paragraph) { Lines.Add(paragraph); return this; }
        public Text B(string item) { Lines.Add(DemoFileFactory.BulletPrefix + item); return this; }
        public Text Row(params string[] cells) { Lines.Add(DemoFileFactory.RowPrefix + string.Join(DemoFileFactory.CellSeparator, cells)); return this; }
        public Text Gap() { Lines.Add(string.Empty); return this; }
        public Text Page() { Lines.Add(DemoFileFactory.PageBreak); return this; }
        public Text Code(params string[] code)
        {
            Lines.Add(DemoFileFactory.CodeFence);
            Lines.AddRange(code);
            Lines.Add(DemoFileFactory.CodeFence);
            return this;
        }
        public static string Bold(string label) => DemoFileFactory.BoldMark + label + DemoFileFactory.BoldMark;
    }

    /// <summary>One line of facts at the top: "**Owner:** Maya Chen · **Status:** Draft".</summary>
    private static string Meta(params (string Label, string Value)[] facts) =>
        string.Join(" · ", facts.Select(f => $"{Text.Bold(f.Label + ":")} {f.Value}"));

    // ------------------------------------------------------------------ description and AI summary

    public static string Description(DemoDoc d)
    {
        var f = new Facts(d.Title);
        var s = d.Subject;
        return d.Kind switch
        {
            DemoKind.MeetingNotes => $"Notes from \"{d.Title}\": attendees, decisions and action items.",
            DemoKind.Contract => $"Agreement with {s}: scope, term, fees and service levels.",
            DemoKind.Spec => $"Specification: {s}. Problem, goals, API and rollout.",
            DemoKind.ReleaseNotes => $"What is new, improved and fixed in mobile app version {s}.",
            DemoKind.Budget => $"FY2026 budget for {s}: spend by quarter, assumptions and risks.",
            DemoKind.Policy => $"Company policy: {s.ToLowerInvariant()}. Who it applies to and what the rules are.",
            DemoKind.Postmortem => $"Blameless postmortem of the {s.ToLowerInvariant()}: impact, timeline, root cause and actions.",
            DemoKind.Guide => $"Step-by-step guide: {s.ToLowerInvariant()}.",
            DemoKind.Report => $"{s}: key figures, commentary and next steps.",
            DemoKind.Proposal => $"{s}: background, objectives, approach, timeline and budget.",
            DemoKind.Invoice => $"Monthly hosting invoice from {s}, payable in 30 days.",
            DemoKind.LogExport => $"Raw export: {s.ToLowerInvariant()}, {f.N(1, 40, 90)} entries.",
            DemoKind.Research => $"Research: {s.ToLowerInvariant()}. Method, findings and recommendations.",
            DemoKind.Press => $"Press release announcing {s.Replace(" launch", "")}, with quotes and availability.",
            DemoKind.JobDescription => $"Open role: {s}. Responsibilities, requirements, salary range and process.",
            DemoKind.BarChart => $"Bar chart: {s.ToLowerInvariant()}.",
            DemoKind.LineChart => $"Line chart: {s.ToLowerInvariant()}.",
            DemoKind.Wireframe => $"Low-fidelity wireframe: {s.ToLowerInvariant()}.",
            DemoKind.Diagram => $"Diagram: {s.ToLowerInvariant()}.",
            _ => $"Chart: {s.ToLowerInvariant()}.",
        };
    }

    /// <summary>The seeded AI summary and its key points ("• " lines), naming the document's own facts.</summary>
    public static (string Summary, string KeyPoints) AiSummary(DemoDoc d)
    {
        var f = new Facts(d.Title);
        var s = d.Subject;
        var people = People(d);
        (string, string[]) pair = d.Kind switch
        {
            DemoKind.MeetingNotes => ($"The meeting agreed on {f.Pick(1, "the scope for the next release", "a new owner for the onboarding work", "moving the review to next week", "a smaller first milestone")} and assigned {f.N(2, 3, 5)} action items. One question stays open until the follow-up.",
                new[] { $"{f.N(2, 3, 5)} action items with owners and due dates", $"Decision recorded by {people[0]}", "Follow-up meeting planned" }),
            DemoKind.Contract => ($"A service agreement with {s} for about {Money(f.N(3, 18, 60) * 100)} a month. It defines a 99.9% availability target, a liability cap and how either side can end the agreement.",
                new[] { $"Monthly fee about {Money(f.N(3, 18, 60) * 100)}", "99.9% availability with service credits", "Liability capped at 12 months of fees", "30 days' notice to terminate" }),
            DemoKind.Spec => ($"The specification describes {s.ToLowerInvariant()}: the problem it solves, three goals and the API it adds. Open questions are listed with owners, and the rollout happens in two stages.",
                new[] { "Three goals, two explicit non-goals", $"{f.N(4, 3, 5)} API endpoints", "Rollout to 10% of users first" }),
            DemoKind.ReleaseNotes => ($"Version {s} of the mobile app adds {f.Pick(5, "offline drafts", "faster sync", "a new onboarding", "dark mode")} and fixes {f.N(6, 3, 6)} bugs. Upgrading needs no manual steps.",
                new[] { $"New: {f.Pick(5, "offline drafts", "faster sync", "a new onboarding", "dark mode")}", $"{f.N(6, 3, 6)} bug fixes", "No breaking changes" }),
            DemoKind.Budget => ($"The FY2026 budget for {s} totals about {Money(f.N(7, 40, 180) * 10000)}. People costs are the largest line; travel was cut after the first review.",
                new[] { $"Total about {Money(f.N(7, 40, 180) * 10000)}", "People costs are 70% of spend", "Travel reduced by 10%", "Two risks tracked" }),
            DemoKind.Policy => ($"The policy explains who the rules on {s.ToLowerInvariant()} apply to, what is allowed and how to ask for an exception. Questions go to {people[0]}.",
                new[] { "Applies to employees and contractors", "Clear limits and approval steps", "Exceptions need written approval" }),
            DemoKind.Postmortem => ($"The {s.ToLowerInvariant()} lasted {f.N(8, 20, 95)} minutes and affected about {f.N(9, 5, 30)}% of users. The root cause was {f.Pick(10, "an expired certificate", "a slow database query after a deploy", "a full disk on a queue worker", "a misconfigured load balancer rule")}.",
                new[] { $"Duration {f.N(8, 20, 95)} minutes", $"About {f.N(9, 5, 30)}% of users affected", $"{f.N(11, 3, 5)} action items, owners assigned" }),
            DemoKind.Guide => ($"A practical guide to {s.ToLowerInvariant()} in {f.N(12, 5, 9)} steps, with checks before and after, and who to call when something goes wrong.",
                new[] { $"{f.N(12, 5, 9)} numbered steps", "Checks before and after", "Escalation contacts listed" }),
            DemoKind.Report => ($"{s} shows {f.Pick(13, "revenue ahead of plan", "steady growth", "costs under control", "higher churn than expected")}. Key figures are compared with the previous period, with commentary and next steps.",
                new[] { $"Revenue about {Money(f.N(14, 80, 240) * 10000)}", $"Growth {f.N(15, 3, 18)}% on the previous period", "Next steps agreed" }),
            DemoKind.Proposal => ($"{s} proposes {f.Pick(16, "a phased approach", "a small first release", "a six-week project", "a joint team")} with a budget of about {Money(f.N(17, 20, 90) * 1000)}. The timeline has three phases and named owners.",
                new[] { "Three phases with owners", $"Budget about {Money(f.N(17, 20, 90) * 1000)}", "Success measured after launch" }),
            DemoKind.Invoice => ($"Invoice from {s} for {Money(f.N(18, 20, 45) * 100)} plus VAT for hosting and support, due within 30 days.",
                new[] { $"Total {Money(f.N(18, 20, 45) * 100)} plus VAT", "Payment due in 30 days" }),
            DemoKind.LogExport => ($"A raw export with {f.N(1, 40, 90)} entries. Most are routine; {f.N(19, 2, 6)} errors stand out and point to the same component.",
                new[] { $"{f.N(1, 40, 90)} entries", $"{f.N(19, 2, 6)} errors in one component" }),
            DemoKind.Research => ($"Research on {s.ToLowerInvariant()} with {f.N(20, 6, 12)} participants. The main finding: {f.Pick(21, "pricing is hard to compare", "setup takes too long", "people want offline access", "reports are hard to find")}.",
                new[] { $"{f.N(20, 6, 12)} participants", "Findings grouped by theme", "Three recommendations" }),
            DemoKind.Press => ($"The press release announces {s.Replace(" launch", "")}, quotes the CEO and a customer, and gives the availability date and pricing.",
                new[] { "Available to all plans", "Quote from a customer", "Contact for media" }),
            DemoKind.JobDescription => ($"An open role for a {s}: what the person will do, what they need to bring, and the salary range.",
                new[] { "Hybrid, two office days a week", $"Salary {Money(f.N(22, 55, 95) * 1000)}–{Money(f.N(22, 55, 95) * 1000 + 20000)}", "Four interview steps" }),
            _ => ($"A picture: {s.ToLowerInvariant()}. It has no text for the AI to read; this summary was written by the uploader.",
                new[] { "Image document", "Shown as a picture in the preview" }),
        };
        return (pair.Item1, string.Join("\n", pair.Item2.Select(p => "• " + p)));
    }

    // ------------------------------------------------------------------ the text of one version

    private static void ReportOther(Text t, DemoDoc d, Facts f, int version, IReadOnlyList<string> people, DemoVersionInfo current, IReadOnlyList<DemoVersionInfo> history)
    {
        bool V(int n) => version >= n;
        var kind = ReportKind(d);
        t.P(Meta(("Prepared by", people[0]), ("Date", Day(current.Date)), ("Status", V(5) ? "Final" : "Draft")));
        switch (kind)
        {
            case "findings":
                t.H("Summary");
                t.P($"The review of {d.Subject.ToLowerInvariant()} found {f.N(140, 3, 6)} issues, none critical. Most are about process rather than technology.");
                t.H("Findings");
                t.Row("ID", "Finding", "Severity", "Status");
                t.Row("F-01", "Leavers keep access to two internal tools for up to a week", "High", V(4) ? "Fixed" : "Open");
                t.Row("F-02", "Admin roles are not reviewed every quarter", "Medium", V(4) ? "In progress" : "Open");
                t.Row("F-03", "Backups are encrypted but the keys are not rotated", "Medium", "Open");
                t.Row("F-04", "Two service accounts have passwords that never expire", V(2) ? "Medium" : "Low", "Open");
                t.Gap();
                break;
            case "slo":
                t.H("Service level objectives");
                t.Row("Service", "Objective", "Actual", "Error budget left");
                t.Row("API", "99.9%", V(4) ? "99.93%" : "99.91%", V(4) ? "30%" : "10%");
                t.Row("Web app", "99.9%", "99.96%", "60%");
                t.Row("Sync", "99.5%", "99.71%", "42%");
                t.Gap();
                break;
            case "hiring":
                t.H("Roles");
                t.Row("Role", "Team", "Start", "Status");
                t.Row("Senior Backend Engineer", "Engineering", "Q1", V(2) ? "Interviewing" : "Open");
                t.Row("Product Designer", "Product", "Q1", "Open");
                t.Row("Customer Success Manager", "Customer Success", V(4) ? "Q1" : "Q2", "Open");
                t.Row("Data Analyst", "Operations", "Q2", "Approved");
                t.Gap();
                break;
            case "survey":
                t.H("Results");
                t.Row("Question", "Score (1-5)", "Responses");
                t.Row("The kick-off call was useful", "4.4", f.N(141, 40, 90).ToString(Inv));
                t.Row("Setup was easy", V(4) ? "3.7" : "3.6", f.N(142, 40, 90).ToString(Inv));
                t.Row("I knew who to ask for help", "4.1", f.N(143, 40, 90).ToString(Inv));
                t.Gap();
                break;
            default:
                t.H("Agenda");
                t.B("Welcome and wins of the month (10 min)");
                t.B("Product update and demo (15 min)");
                t.B("Numbers: revenue, customers, hiring (10 min)");
                if (V(2)) t.B("Questions from the team (15 min)");
                break;
        }
        t.H("Commentary");
        t.P(kind switch
        {
            "findings" => "The access findings come from manual steps in the leaver process. Automating them closes F-01 and F-02.",
            "slo" => "The API used most of its budget in one incident; the other services are well within their objectives.",
            "hiring" => "The backend role is the most urgent: the mobile app work depends on it.",
            "survey" => "Setup is still the weakest step. The new invite flow should help.",
            _ => "Keep it short: the recording and the slides go out after the meeting.",
        });
        if (V(3)) t.P("The main change since the last version is reflected in the table above.");
        if (d.Type == "pdf")
        {
            t.Page();
            t.H("Appendix: version history");
            foreach (var h in history.Where(h => h.Number <= version)) t.P($"v{h.Number} - {h.Date:yyyy-MM-dd} - {h.Author}: {h.Summary}");
        }
    }

    public static IReadOnlyList<string> Lines(DemoDoc d, int version, IReadOnlyList<DemoVersionInfo> history)
    {
        var f = new Facts(d.Title);
        var people = People(d);
        var first = history.Count > 0 ? history[0].Date : DateTime.UtcNow;
        var current = history.FirstOrDefault(h => h.Number == version) ?? new DemoVersionInfo(version, people[0], first, ChangeSummary(d, version));
        bool V(int n) => version >= n;
        var t = new Text();

        switch (d.Kind)
        {
            case DemoKind.MeetingNotes:
            {
                t.P(Meta(("Date", Day(first)), ("Facilitator", people[0]), ("Notes by", current.Author)));
                t.H("Attendees");
                var count = Math.Min(people.Count, V(4) ? 5 : 4);
                for (var i = 0; i < count; i++) t.B(people[i]);
                t.H("Agenda");
                t.B(f.Pick(30, "Status of the current milestone", "Review of last week's actions", "Customer feedback from the past week"));
                t.B(f.Pick(31, "Scope for the next release", "Risks and blockers", "Hiring and capacity"));
                t.B("Any other business");
                t.H("Decisions");
                t.B(V(6) ? "Scope: the first milestone covers sign-up and the dashboard only; reports move to the second." : "Scope: the first milestone covers sign-up and the dashboard.");
                t.B($"{people[Math.Min(1, people.Count - 1)]} owns the follow-up with the customer.");
                t.H("Action items");
                t.Row("Owner", "Action", "Due", "Status");
                t.Row(people[0], "Share the updated plan", Day(first.AddDays(3)), V(3) ? "Done" : "Open");
                t.Row(people[Math.Min(1, people.Count - 1)], "Book the follow-up meeting", Day(first.AddDays(5)), "Open");
                if (V(2)) t.Row(people[Math.Min(2, people.Count - 1)], "Collect the open questions in one list", Day(first.AddDays(7)), "Open");
                t.Gap();
                if (V(5)) t.P($"{Text.Bold("Follow-up:")} {Day(first.AddDays(14))}, same time.");
                break;
            }

            case DemoKind.Contract:
            {
                var fee = f.N(3, 18, 60) * 100m + (V(2) ? 150 : 0);
                t.P(Meta(("Parties", $"DocFlowHub Ltd. and {d.Subject}"), ("Status", V(7) ? "Signed" : V(3) ? "In review" : "Draft")));
                t.H("1. Scope");
                t.P($"{d.Subject} provides the services described in Schedule A, including setup, operation and support during business hours.");
                t.H("2. Term");
                t.P($"The agreement runs for {(V(4) ? 24 : 12)} months from {Day(first.AddDays(10))} and renews for 12 months unless either party gives notice.");
                t.H("3. Fees");
                t.Row("Item", "Monthly fee", "Notes");
                t.Row("Base service", Money(fee), "Billed monthly in advance");
                t.Row("Premium support", Money(f.N(23, 3, 9) * 100), "Optional");
                t.Row("Additional storage", Money(f.N(24, 1, 4) * 50), "Per 100 GB");
                t.Gap();
                t.H("4. Service levels");
                t.B("Availability of 99.9% per calendar month.");
                t.B("Response to critical incidents within 1 hour.");
                t.B("Service credits of 5% of the monthly fee for each 0.1% below target.");
                t.H("5. Liability");
                t.P(V(6)
                    ? "Each party's total liability is capped at the fees paid in the 12 months before the claim. The cap does not apply to breaches of confidentiality or data protection."
                    : "Each party's liability is limited to the fees paid under this agreement.");
                t.H("6. Termination");
                t.B("Either party may terminate with 30 days' written notice.");
                if (!V(5)) t.B("Termination in the first year carries an early-termination fee of three months' fees.");
                t.B("Either party may terminate at once for a material breach that is not fixed within 14 days.");
                if (V(3))
                {
                    t.H("7. Data retention");
                    t.P($"{d.Subject} keeps customer data only for the term of the agreement and deletes it within 30 days of its end, unless the law requires otherwise.");
                }
                if (V(7)) t.P($"*Signed on {Day(current.Date)} by {people[0]} and the authorised representative of {d.Subject}.*");
                break;
            }

            case DemoKind.Spec:
            {
                t.P(Meta(("Owner", people[0]), ("Status", V(6) ? "Approved" : V(3) ? "In review" : "Draft"), ("Last update", Day(current.Date))));
                t.H("Problem");
                t.P($"Today {f.Pick(40, "users lose their work when the connection drops", "teams cannot see what changed between releases", "notifications arrive late or twice", "the onboarding takes too long on small screens")}. Support handles about {f.N(41, 20, 80)} tickets a month about it.");
                t.H("Goals");
                t.B("Make the main flow work in under two seconds on a mid-range phone.");
                t.B("Keep the data model compatible with the web app.");
                if (V(2)) t.B("Let people keep working offline and sync when they are back online.");
                t.B("Ship behind a feature flag.");
                t.H("Non-goals");
                t.B("A redesign of the settings screens.");
                t.B("Support for tablets in the first release.");
                t.H("API");
                t.Row("Method", "Path", "Purpose");
                t.Row("GET", "/api/v2/items", "List items changed since a timestamp");
                t.Row("POST", "/api/v2/items", "Create or update an item");
                t.Row("DELETE", "/api/v2/items/{id}", "Delete an item");
                if (V(3)) t.Row("POST", "/api/v2/items/batch", "Send up to 100 changes at once");
                t.Gap();
                if (d.Type == "md")
                {
                    t.P("Example response:");
                    t.Code("{", "  \"items\": [{ \"id\": 42, \"title\": \"Draft\", \"updatedAt\": \"2026-10-01T09:30:00Z\" }],", $"  \"next\": \"{f.N(42, 1000, 9999)}\"", "}");
                }
                t.H("Open questions");
                if (!V(4)) t.B("What rate limit do we apply per device?");
                t.B("Do we keep deleted items for 30 or 90 days?");
                if (V(5))
                {
                    t.H("Rollout");
                    t.B("Stage 1: internal users and 10% of customers for two weeks.");
                    t.B("Stage 2: everyone, if the crash-free rate stays above 99.5%.");
                }
                if (V(7))
                {
                    t.H("Success metrics");
                    t.Row("Metric", "Today", "Target");
                    t.Row("Sync time (p95)", $"{f.N(43, 4, 9)} s", "2 s");
                    t.Row("Related tickets per month", f.N(41, 20, 80).ToString(Inv), "Under 10");
                    t.Gap();
                }
                break;
            }

            case DemoKind.ReleaseNotes:
            {
                t.P(Meta(("Released", Day(first)), ("Build", f.N(50, 1200, 1900).ToString(Inv)), ("Platforms", "iOS and Android")));
                t.H("New");
                t.B(f.Pick(5, "Offline drafts: keep editing without a connection.", "Faster sync: changes arrive in seconds.", "A new onboarding with three short steps.", "Dark mode follows the system setting."));
                t.B("Share a document straight from the preview.");
                t.H("Improved");
                t.B($"App start is {f.N(51, 15, 40)}% faster on older phones.");
                t.B("Clearer error messages when an upload fails.");
                t.H("Fixed");
                t.B("The keyboard no longer hides the comment field.");
                t.B("Dates show in the phone's time zone.");
                t.B("Pull to refresh works on empty lists.");
                if (V(2)) t.B("Hotfix: some users were sent back to the sign-in screen in a loop.");
                t.H("Upgrade notes");
                t.P(V(3) ? "No action is needed. The first start after the update migrates local data; it takes a few seconds." : "No action is needed.");
                t.Code($"dfh-mobile migrate --to {d.Subject}", "dfh-mobile verify");
                break;
            }

            case DemoKind.Budget:
            {
                decimal Scale(decimal v) => v * f.N(7, 40, 180) / 100m;
                var people_ = new[] { 220m, 225m, 230m, 240m }.Select(v => Scale(v * 1000) * (V(4) ? 1.08m : 1m)).ToArray();
                var travel = new[] { 18m, 22m, 15m, 25m }.Select(v => Scale(v * 1000) * (V(2) ? 0.9m : 1m)).ToArray();
                var tools = new[] { 12m, 12m, 13m, 13m }.Select(v => Scale(v * 1000)).ToArray();
                var other = new[] { 9m, 8m, 11m, 10m }.Select(v => Scale(v * 1000)).ToArray();
                if (V(7)) { other[3] -= Scale(4000); other[0] += Scale(4000); }
                string Row(string name, decimal[] q) => string.Join(DemoFileFactory.CellSeparator, new[] { name }.Concat(q.Select(Money)).Append(Money(q.Sum())));
                var total = people_.Sum() + travel.Sum() + other.Sum() + (V(3) ? tools.Sum() : 0);

                t.P(Meta(("Department", d.Subject), ("Owner", people[0]), ("Status", V(6) ? "Approved" : V(5) ? "Final" : "Draft")));
                t.H("Summary");
                t.P($"The {d.Subject} budget for FY2026 is {Money(total)}. People costs are the largest line; the plan keeps spend flat in real terms.");
                t.H("Budget by quarter");
                t.Row("Category", "Q1", "Q2", "Q3", "Q4", "Total");
                t.Lines.Add(DemoFileFactory.RowPrefix + Row("People", people_));
                t.Lines.Add(DemoFileFactory.RowPrefix + Row("Travel", travel));
                if (V(3)) t.Lines.Add(DemoFileFactory.RowPrefix + Row("Tools and licences", tools));
                t.Lines.Add(DemoFileFactory.RowPrefix + Row("Other", other));
                t.Gap();
                t.H("Assumptions");
                t.B(V(4) ? "Two new hires start in Q2." : "No new hires before Q3.");
                t.B("Salaries rise by 3% in April.");
                t.B("Travel stays at the FY2025 level per person.");
                t.H("Risks");
                t.B("Currency changes on supplier contracts paid in USD.");
                t.B("Delays in hiring move people costs to later quarters.");
                break;
            }

            case DemoKind.Policy:
            {
                t.P(Meta(("Owner", people[0]), ("Status", V(5) ? "Approved" : "Draft"), ("Applies from", Day(first.AddDays(30)))));
                t.H("1. Purpose");
                t.P($"This policy sets out the rules on {d.Subject.ToLowerInvariant().Replace(" policy", "")} so that everyone knows what is expected and decisions are fair and consistent.");
                t.H("2. Scope");
                t.P(V(2) ? "It applies to all employees and to contractors working for the company for more than one month." : "It applies to all employees.");
                t.H("3. Rules");
                if (IsTravel(d))
                {
                    t.B("Book travel at least 14 days ahead where possible.");
                    t.B($"Daily meal allowance when travelling: {Money(V(3) ? 55 : 45)}.");
                    t.B("Economy class for flights under six hours.");
                    t.B("Keep receipts for every expense above $25.");
                }
                else
                {
                    t.B(f.Pick(130, "Use company accounts and devices for company work.", "Treat colleagues and customers with respect.", "Agree your working hours with your manager.", "Keep customer data inside company systems."));
                    t.B(f.Pick(131, "Report a lost device within 24 hours.", "Raise concerns early, with your manager or HR.", "Be reachable during the team's core hours.", "Use multi-factor sign-in everywhere."));
                    t.B("Ask before installing software that is not on the approved list.");
                }
                t.H("4. Responsibilities");
                t.P($"Managers answer requests within {(V(3) && !IsTravel(d) ? "three" : "five")} working days. People Operations reviews the policy once a year.");
                if (V(4))
                {
                    t.H("5. Exceptions");
                    t.P("An exception needs written approval from the department head and Finance. Approved exceptions are recorded with the reason.");
                }
                t.H(V(4) ? "6. Questions" : "5. Questions");
                t.P($"Questions about this policy go to {(V(6) ? people[Math.Min(1, people.Count - 1)] : people[0])}.");
                if (d.Type == "pdf")
                {
                    t.Page();
                    t.H("Appendix: version history");
                    foreach (var h in history.Where(h => h.Number <= version)) t.P($"v{h.Number} - {h.Date:yyyy-MM-dd} - {h.Author}: {h.Summary}");
                }
                break;
            }

            case DemoKind.Postmortem:
            {
                var start = first.Date.AddHours(9).AddMinutes(f.N(60, 0, 50));
                var minutes = f.N(8, 20, 95);
                t.P(Meta(("Severity", f.Pick(61, "SEV-1", "SEV-2", "SEV-2", "SEV-3")), ("Duration", $"{minutes} min"), ("Status", V(3) ? "Final" : "Draft")));
                t.H("Summary");
                t.P($"On {Day(first)} the {d.Subject.ToLowerInvariant()} affected about {f.N(9, 5, 30)}% of users for {minutes} minutes.");
                t.H("Impact");
                t.B($"{f.N(62, 300, 4000).ToString("N0", Inv)} failed requests.");
                t.B($"{f.N(63, 5, 40)} support tickets.");
                t.B("No data was lost.");
                t.H("Timeline");
                t.Row("Time (UTC)", "Event");
                t.Row(start.AddMinutes(V(2) ? -6 : 0).ToString("HH:mm", Inv), "First alerts fire");
                t.Row(start.AddMinutes(8).ToString("HH:mm", Inv), "On-call engineer acknowledges");
                t.Row(start.AddMinutes(minutes / 2).ToString("HH:mm", Inv), "Cause identified");
                t.Row(start.AddMinutes(minutes).ToString("HH:mm", Inv), "Service restored");
                t.Gap();
                t.H("Root cause");
                t.P(V(3) ? $"The cause was {f.Pick(10, "an expired certificate on the internal gateway", "a slow database query introduced by a deploy", "a full disk on a queue worker", "a misconfigured load balancer rule")}. Monitoring did not cover it." : "Under investigation.");
                t.H("Action items");
                t.Row("Owner", "Action", "Status");
                t.Row(people[0], "Add an alert for the failing component", V(4) ? "Done" : "Open");
                t.Row(people[Math.Min(1, people.Count - 1)], "Add a check to the deploy pipeline", V(4) ? "Done" : "Open");
                t.Row(people[Math.Min(2, people.Count - 1)], "Update the runbook", "Open");
                t.Gap();
                if (V(5))
                {
                    t.H("Lessons learned");
                    t.B("Alerts on symptoms caught it; alerts on causes would have caught it earlier.");
                    t.B("The runbook step for rollback saved time.");
                }
                break;
            }

            case DemoKind.Guide:
            {
                t.P(Meta(("Owner", people[0]), ("Last reviewed", Day(current.Date))));
                var ops = IsOps(d);
                t.H("Before you start");
                if (ops)
                {
                    t.B("Make sure you have access to the admin console.");
                    if (V(2)) t.B("Check that last night's backups finished.");
                    t.B("Tell the team in the incident channel.");
                }
                else
                {
                    t.B("Read the whole guide once before you begin.");
                    if (V(2)) t.B("Check that the latest version of the template is the one you use.");
                    t.B("Agree on one owner for the whole process.");
                }
                t.H("Steps");
                var steps = f.N(12, 5, 7);
                var pool = ops
                    ? new[] { "open the console and select the environment", "confirm the current state on the status page", "run the command below and wait for it to finish", "check the result in the dashboard", "note the time and what you did in the log", "ask a second person to review the change", "close the task and update the ticket" }
                    : new[] { "confirm the date and the people involved", "share the agenda or the draft one week ahead", "prepare the materials and send them in advance", "book the room or the video call", "write down the decisions and their owners", "send a short summary within 24 hours", "collect feedback and update this guide" };
                for (var i = 1; i <= steps; i++)
                {
                    t.B($"Step {i}: " + pool[(i - 1 + f.N(70, 0, 6)) % pool.Length]);
                }
                if (ops && d.Type == "md")
                {
                    t.Code(V(3) ? $"dfh ops run --env production --task {f.Pick(80, "failover", "renew", "verify", "rotate")}" : $"dfh-ops {f.Pick(80, "failover", "renew", "verify", "rotate")} production", "dfh ops status");
                }
                if (V(4))
                {
                    t.H("Troubleshooting");
                    t.B(ops ? "If the command times out, run it again; it is safe to repeat." : "If people cannot make the date, offer two other slots at once.");
                    t.B(ops ? "If the dashboard does not update in 5 minutes, escalate." : "If the materials are late, send what you have and say what follows.");
                }
                if (V(6))
                {
                    t.H("Escalation");
                    t.Row("Who", "When");
                    t.Row(people[0], "First contact");
                    t.Row(people[Math.Min(1, people.Count - 1)], "If there is no answer in 15 minutes");
                    t.Gap();
                }
                break;
            }

            case DemoKind.Report when ReportKind(d) != "kpi":
            {
                ReportOther(t, d, f, version, people, current, history);
                break;
            }

            case DemoKind.Report:
            {
                var revenue = f.N(14, 80, 240) * 10000m * (V(2) ? 1.02m : 1m);
                var growth = f.N(15, 3, 18);
                var headcount = f.N(90, 40, 120) + (V(4) ? 2 : 0);
                t.P(Meta(("Period", d.Subject != d.Title ? d.Subject : f.Pick(91, "FY2026", "the first half of 2026")), ("Prepared by", people[0]), ("Status", V(7) ? "Final" : "Draft")));
                t.H("Highlights");
                t.B($"Revenue of {Money(revenue)}, {growth}% above the previous period.");
                t.B($"Gross margin {f.N(92, 68, 82)}%.");
                t.B($"Headcount {headcount}.");
                t.H("Key figures");
                t.Row("Metric", "This period", "Previous", "Change");
                t.Row("Revenue", Money(revenue), Money(revenue * 100 / (100 + growth)), $"+{growth}%");
                t.Row("New customers", f.N(93, 20, 90).ToString(Inv), f.N(94, 15, 80).ToString(Inv), "");
                t.Row("Churn", $"{f.N(95, 1, 4)}.{f.N(96, 0, 9)}%", $"{f.N(97, 1, 3)}.{f.N(98, 0, 9)}%", "");
                t.Row("Headcount", headcount.ToString(Inv), (headcount - f.N(99, 1, 6)).ToString(Inv), "");
                t.Gap();
                t.H("Commentary");
                t.P("Growth came mostly from existing customers moving to larger plans. New business was in line with the plan.");
                if (V(3)) t.P("Churn rose in the second month after a price change for the smallest plan; it went back to normal after the change was rolled back.");
                if (V(5))
                {
                    t.H("Next steps");
                    t.B("Review the pricing of the smallest plan.");
                    t.B("Start the hiring for the two open roles.");
                    t.B("Report on the churn experiment next period.");
                }
                if (d.Type == "pdf")
                {
                    // A second page of detail, so PDF reports show page navigation (three pages with the appendix).
                    t.Page();
                    t.H("Detail by month");
                    t.Row("Month", "Revenue", "New customers");
                    var month = new DateTime(2026, 1, 1);
                    for (var i = 0; i < 12; i++)
                    {
                        t.Row(month.AddMonths(i).ToString("MMMM", Inv), Money(Math.Round(revenue / 12 * (0.85m + f.N(150 + i, 0, 30) / 100m), 0)), f.N(160 + i, 3, 14).ToString(Inv));
                    }
                    t.Gap();
                    t.Page();
                    t.H("Appendix: version history");
                    foreach (var h in history.Where(h => h.Number <= version)) t.P($"v{h.Number} - {h.Date:yyyy-MM-dd} - {h.Author}: {h.Summary}");
                }
                break;
            }

            case DemoKind.Proposal:
            {
                var launch = first.AddDays(f.N(100, 60, 120) + (V(3) ? 14 : 0));
                t.P(Meta(("Sponsor", people[0]), ("Status", V(6) ? "Approved" : "Proposal"), ("Target date", Day(launch))));
                t.H("Background");
                t.P($"{d.Subject} responds to {f.Pick(101, "feedback from the last two quarters", "a drop in sign-ups on mobile", "the launch of a competing product", "the board's goals for next year")}. This document proposes how to get there.");
                t.H("Objectives");
                t.B($"Raise {f.Pick(102, "trial sign-ups", "conversion", "activation", "awareness")} by {f.N(103, 10, 30)}%.");
                t.B("Ship a first version within one quarter.");
                if (!V(2)) t.B("Cover every market in the first release.");
                t.H("Approach");
                t.B("Phase 1: research and a clear brief.");
                if (V(5)) t.B("Phase 2: user testing with ten customers.");
                t.B($"Phase {(V(5) ? 3 : 2)}: build and launch.");
                t.B($"Phase {(V(5) ? 4 : 3)}: measure and improve.");
                t.H("Timeline");
                t.Row("Phase", "Dates", "Owner");
                t.Row("Research", $"{Day(first.AddDays(7))} – {Day(first.AddDays(28))}", V(7) ? people[Math.Min(2, people.Count - 1)] : people[0]);
                t.Row("Build", $"{Day(first.AddDays(29))} – {Day(launch.AddDays(-7))}", people[Math.Min(1, people.Count - 1)]);
                t.Row("Launch", Day(launch), people[0]);
                t.Gap();
                if (V(4))
                {
                    t.H("Budget");
                    t.P($"About {Money(f.N(17, 20, 90) * 1000)}, of which 60% is agency and freelance work.");
                }
                break;
            }

            case DemoKind.Invoice:
            {
                var vat = V(2) ? 20 : 23;
                var hosting = f.N(18, 20, 45) * 100m;
                var support = f.N(110, 2, 6) * 100m;
                var net = hosting + support;
                t.P(Meta(("Invoice", d.Title.Split(' ')[1]), ("Date", Day(first)), ("Due", Day(first.AddDays(30)))));
                t.P($"From: {d.Subject}, 10 Harbour Street, London");
                t.P("To: DocFlowHub Ltd., Accounts Payable");
                if (V(3)) t.P($"Purchase order: PO-{f.N(111, 4000, 4999)}");
                t.H("Items");
                t.Row("Item", "Qty", "Unit price", "Amount");
                t.Row("Managed hosting, production", "1", Money(hosting), Money(hosting));
                t.Row("Support hours", f.N(110, 2, 6).ToString(Inv), "$100", Money(support));
                t.Gap();
                t.P($"{Text.Bold("Net:")} {Money(net)} · {Text.Bold($"VAT {vat}%:")} {Money(net * vat / 100)} · {Text.Bold("Total:")} {Money(net * (100 + vat) / 100)}");
                t.P("Payment by bank transfer within 30 days. Please quote the invoice number.");
                break;
            }

            case DemoKind.LogExport:
            {
                var entries = f.N(1, 40, 90) + (V(2) ? 20 : 0);
                var at = first.Date.AddHours(8);
                var rng = new Random(f.Seed);
                var levels = new[] { "INFO", "INFO", "INFO", "INFO", "WARN", "ERROR" };
                var messages = new[] { "request completed", "cache refreshed", "job started", "job finished", "slow query detected", "upstream timeout" };
                var users = new[] { "user-1042", "user-2210", "user-3381", "user-4479" };
                for (var i = 0; i < entries; i++)
                {
                    at = at.AddSeconds(rng.Next(5, 600));
                    var level = rng.Next(levels.Length);
                    var who = V(3) ? "user-***" : users[rng.Next(users.Length)];
                    t.P($"{at:yyyy-MM-dd HH:mm:ss} {levels[level],-5} {f.Pick(112, "api-gateway", "sync-worker", "export-service", "auth")} {messages[level]} {who} {rng.Next(12, 900)}ms");
                }
                break;
            }

            case DemoKind.Research:
            {
                var participants = f.N(20, 6, 12) + (V(2) ? 2 : 0);
                t.P(Meta(("Researcher", people[0]), ("Participants", participants.ToString(Inv)), ("Method", "Remote interviews, 45 minutes")));
                t.H("Participants");
                t.Row("Segment", "Count");
                t.Row("Small teams (under 20)", (participants / 2).ToString(Inv));
                t.Row("Mid-size companies", (participants - participants / 2).ToString(Inv));
                t.Gap();
                t.H(V(3) ? "Findings by theme" : "Findings");
                if (V(3)) t.P(Text.Bold("Getting started"));
                t.B($"Setup takes too long: {f.N(120, 4, 8)} of {participants} needed help to invite their team.");
                t.P("*\"I gave up the first time and came back a week later.\"*");
                if (V(3)) t.P(Text.Bold("Everyday use"));
                t.B("Search is the main way people find documents; folders matter less than expected.");
                t.B($"{f.N(121, 3, 6)} participants asked for offline access on the phone.");
                if (V(4))
                {
                    t.H("Recommendations");
                    t.B("Shorten the invite flow to one screen.");
                    t.B("Make search the first thing on the dashboard.");
                    t.B("Explore an offline mode for the mobile app.");
                }
                break;
            }

            case DemoKind.Press:
            {
                var available = first.AddDays(V(3) ? 21 : 14);
                t.P(Text.Bold($"DocFlowHub launches {d.Subject.Replace(" launch", "")}, analytics for every document team"));
                t.P($"*London, {Day(first)}*");
                t.P($"DocFlowHub today announced {d.Subject.Replace(" launch", "")}, an add-on that shows how documents move through a team: what is read, what is stuck in review and which versions changed the most.");
                if (V(2)) t.P("*\"Teams told us they could store their documents but not see their work. Atlas closes that gap,\" said the CEO of DocFlowHub.*");
                t.P("*\"We found three slow review steps in the first week,\" said an early customer.*");
                t.H("Availability");
                t.P($"{d.Subject.Replace(" launch", "")} is available from {Day(available)} on all paid plans, at no extra cost until the end of the year.");
                t.H("About DocFlowHub");
                t.P("DocFlowHub helps teams store, version and understand their documents. It is used by more than 2,000 teams.");
                if (V(4)) t.P($"Media contact: {people[0]}, press@example.com");
                break;
            }

            case DemoKind.JobDescription:
            {
                var low = f.N(22, 55, 95) * 1000m + (V(2) ? 5000 : 0);
                t.P(Meta(("Team", d.Team ?? "Operations"), ("Location", "Hybrid, London"), ("Hiring manager", people[0])));
                t.H("About the role");
                t.P($"We are looking for a {d.Subject} to join a small team that ships often and talks to customers every week.");
                t.H("What you will do");
                t.B("Own a part of the product from idea to release.");
                t.B("Work closely with design, engineering and customer success.");
                t.B("Help us keep the bar high and the process light.");
                t.H("What you bring");
                t.B("Several years of experience in a similar role.");
                if (!V(3)) t.B("A university degree in a related field.");
                t.B("Clear writing and a habit of sharing work early.");
                t.H("Salary and process");
                t.P($"Salary {Money(low)}–{Money(low + 20000)}, plus equity and a learning budget.");
                t.B("A 30-minute call with the hiring manager.");
                t.B("A short take-home task.");
                t.B("A team interview and a meeting with a founder.");
                break;
            }
        }
        return t.Lines;
    }
}
