using DocFlowHub.Core.Models;

namespace DocFlowHub.Infrastructure.Data.Demo;

/// <summary>What a demo document is, which picks its text template (Sprint 24).</summary>
public enum DemoKind
{
    MeetingNotes,
    Contract,
    Spec,
    ReleaseNotes,
    Budget,
    Policy,
    Postmortem,
    Guide,
    Report,
    Proposal,
    Invoice,
    LogExport,
    Research,
    Press,
    JobDescription,
    BarChart,
    LineChart,
    Wireframe,
    Diagram,
    Donut,
}

/// <summary>A colleague (or one of the two demo accounts: keys "user" and "admin").</summary>
public sealed record DemoPerson(string Key, string FirstName, string LastName, string Title)
{
    public string Name => $"{FirstName} {LastName}";
    public string Email => $"{FirstName}.{LastName}@example.com".ToLowerInvariant();
}

public sealed record DemoTeam(string Name, string Description, (string Person, TeamRole Role)[] Members);

/// <summary>A project; <see cref="Folders"/> are paths, "Wireframes/Mobile" is a subfolder of "Wireframes".</summary>
public sealed record DemoProject(string Name, string Description, string Color, string Icon, string Owner, bool Archived, string[] Folders);

/// <summary>
/// One demo document. The title is its key: the files are rebuilt from the catalogue entry with this
/// title, so titles are unique. <see cref="AgeDays"/> is how long ago version 1 was written.
/// </summary>
public sealed record DemoDoc(
    string Title, DemoKind Kind, string Type, string? Project, string? Folder, string Owner, string? Team,
    int Versions, int AgeDays, string[] Categories)
{
    /// <summary>The part after the dash ("Service agreement — Northwind Hosting" → "Northwind Hosting"), or the title.</summary>
    public string Subject => Title.Contains(" — ", StringComparison.Ordinal) ? Title[(Title.IndexOf(" — ", StringComparison.Ordinal) + 3)..] : Title;

    public bool IsPicture => Kind is DemoKind.BarChart or DemoKind.LineChart or DemoKind.Wireframe or DemoKind.Diagram or DemoKind.Donut;
}

/// <summary>
/// The demo workspace of a small software company (Sprint 24): 10 colleagues besides the two demo
/// accounts, 6 teams, 10 projects with their own folders, and about 130 documents written for them.
/// Everything here is fixed data; the seeder adds dates and authors with its seeded random generator.
/// </summary>
public static class DemoCatalog
{
    public const string User = "user";
    public const string Admin = "admin";

    public static readonly DemoPerson[] Colleagues =
    {
        new("maya", "Maya", "Chen", "Product Manager"),
        new("tom", "Tom", "Becker", "Backend Engineer"),
        new("priya", "Priya", "Nair", "Finance Lead"),
        new("lucas", "Lucas", "Martin", "Frontend Engineer"),
        new("sofia", "Sofia", "Rossi", "Marketing Manager"),
        new("jonas", "Jonas", "Berg", "Legal Counsel"),
        new("amara", "Amara", "Okafor", "Customer Success Lead"),
        new("daniel", "Daniel", "Novak", "DevOps Engineer"),
        new("elena", "Elena", "Petrova", "UX Designer"),
        new("sam", "Sam", "Whitaker", "Head of Operations"),
    };

    public static readonly string[] Categories =
    {
        "Reports", "Contracts", "Invoices", "Specifications", "Manuals", "Presentations", "Policies",
        "Meeting notes", "Design", "Research",
    };

    public static readonly DemoTeam[] Teams =
    {
        new("Engineering", "Backend, frontend and infrastructure. Owns the platform, the mobile app and on-call.",
            new[] { ("tom", TeamRole.Admin), ("lucas", TeamRole.Member), ("daniel", TeamRole.Member), (User, TeamRole.Member), (Admin, TeamRole.Admin) }),
        new("Product", "Product management and design: roadmaps, specifications and research.",
            new[] { ("maya", TeamRole.Admin), ("elena", TeamRole.Member), ("lucas", TeamRole.Member), (User, TeamRole.Member) }),
        new("Marketing", "Brand, campaigns, the website and launches.",
            new[] { ("sofia", TeamRole.Admin), ("elena", TeamRole.Member), (User, TeamRole.Member) }),
        new("Finance", "Budgets, forecasts, invoices and the board pack.",
            new[] { ("priya", TeamRole.Admin), ("sam", TeamRole.Member), (Admin, TeamRole.Member) }),
        new("Legal & Compliance", "Contracts, policies, audits and data protection.",
            new[] { ("jonas", TeamRole.Admin), ("priya", TeamRole.Member), (Admin, TeamRole.Admin) }),
        new("Customer Success", "Onboarding, renewals and customer feedback.",
            new[] { ("amara", TeamRole.Admin), ("maya", TeamRole.Member), (User, TeamRole.Member) }),
    };

    public static readonly DemoProject[] Projects =
    {
        new("Website Redesign", "A new public website: clearer pricing, faster pages and a self-serve trial.", "#0284c7", "bi-globe", User, false,
            new[] { "Research", "Wireframes", "Wireframes/Mobile", "Content", "Launch" }),
        new("Mobile App 2.0", "Offline mode, push notifications and a rebuilt onboarding for the iOS and Android apps.", "#7c3aed", "bi-phone", User, false,
            new[] { "Specs", "Design", "Releases", "QA" }),
        new("FY2026 Budget", "Department budgets, the revenue forecast and the board pack for the 2026 financial year.", "#059669", "bi-graph-up", User, false,
            new[] { "Departments", "Forecasts", "Board pack" }),
        new("Customer Onboarding", "Playbooks and templates that get new customers live in their first 30 days.", "#db2777", "bi-person-check", User, false,
            new[] { "Playbooks", "Templates", "Feedback" }),
        new("Product Launch — Atlas", "Launch of Atlas, the analytics add-on: positioning, campaign and press.", "#d97706", "bi-rocket", User, false,
            new[] { "Messaging", "Campaign", "Campaign/Assets", "Press" }),
        new("Platform Reliability", "Postmortems, runbooks and monitoring for the production platform.", "#dc2626", "bi-activity", User, false,
            new[] { "Postmortems", "Runbooks", "Monitoring" }),
        new("Hiring Q1", "Open roles for the first quarter: job descriptions and interview kits.", "#0891b2", "bi-people", User, false,
            new[] { "Job descriptions", "Interview kits" }),
        new("Office Move 2025", "The move to the new office on the third floor. Finished in 2025.", "#64748b", "bi-building", User, true,
            new[] { "Floor plans", "Vendors" }),
        new("Security Audit 2026", "The yearly security audit: scope, findings and evidence.", "#b91c1c", "bi-shield-lock", Admin, false,
            new[] { "Findings", "Evidence" }),
        new("Vendor Management", "Contracts and invoices of the company's suppliers.", "#65a30d", "bi-briefcase", Admin, false,
            new[] { "Contracts", "Invoices" }),
    };

    private static DemoDoc D(string title, DemoKind kind, string type, string? project, string? folder, string owner, string? team,
        int versions, int age, params string[] categories) =>
        new(title, kind, type, project, folder, owner, team, versions, age, categories);

    public static readonly IReadOnlyList<DemoDoc> Documents = Build();

    private static IReadOnlyList<DemoDoc> Build()
    {
        const string web = "Website Redesign", app = "Mobile App 2.0", budget = "FY2026 Budget", onboarding = "Customer Onboarding",
            atlas = "Product Launch — Atlas", ops = "Platform Reliability", hiring = "Hiring Q1", move = "Office Move 2025",
            audit = "Security Audit 2026", vendors = "Vendor Management";
        const string eng = "Engineering", product = "Product", marketing = "Marketing", finance = "Finance", legal = "Legal & Compliance",
            cs = "Customer Success";
        const string md = "md", docx = "docx", pdf = "pdf", txt = "txt", png = "png";

        var docs = new List<DemoDoc>
        {
            // ---- Website Redesign
            D("Website redesign brief", DemoKind.Proposal, docx, web, "Research", User, product, 5, 215, "Design", "Presentations"),
            D("User interview synthesis", DemoKind.Research, md, web, "Research", "elena", product, 3, 192, "Research"),
            D("Competitor review — pricing pages", DemoKind.Research, docx, web, "Research", "maya", product, 2, 186, "Research"),
            D("Homepage wireframe", DemoKind.Wireframe, png, web, "Wireframes", "elena", product, 4, 160, "Design"),
            D("Pricing page wireframe", DemoKind.Wireframe, png, web, "Wireframes", "elena", product, 3, 151, "Design"),
            D("Mobile navigation wireframe", DemoKind.Wireframe, png, web, "Wireframes/Mobile", "elena", product, 2, 122, "Design"),
            D("Website content plan", DemoKind.Spec, md, web, "Content", "sofia", marketing, 3, 112, "Specifications"),
            D("Homepage copy deck", DemoKind.Proposal, docx, web, "Content", "sofia", marketing, 6, 96, "Presentations"),
            D("Website launch checklist", DemoKind.Guide, md, web, "Launch", User, null, 4, 31, "Manuals"),
            D("Site traffic — last 12 months", DemoKind.LineChart, png, web, "Research", "sofia", marketing, 2, 58, "Reports"),

            // ---- Mobile App 2.0
            D("Mobile app 2.0 — product requirements", DemoKind.Spec, docx, app, "Specs", "maya", product, 7, 252, "Specifications"),
            D("Offline sync design", DemoKind.Spec, md, app, "Specs", "tom", eng, 4, 203, "Specifications", "Design"),
            D("Push notifications API", DemoKind.Spec, docx, app, "Specs", "tom", eng, 3, 171, "Specifications"),
            D("App architecture overview", DemoKind.Diagram, png, app, "Design", "daniel", eng, 3, 182, "Design"),
            D("Onboarding flow wireframe", DemoKind.Wireframe, png, app, "Design", "elena", product, 3, 141, "Design"),
            D("QA test plan 2.0", DemoKind.Guide, docx, app, "QA", "lucas", eng, 3, 101, "Manuals"),
            D("Crash report export — October", DemoKind.LogExport, txt, app, "QA", "daniel", eng, 1, 41, "Reports"),
            D("Crash-free sessions", DemoKind.LineChart, png, app, "QA", "daniel", eng, 2, 19, "Reports"),

            // ---- FY2026 Budget
            D("FY2026 budget — Engineering", DemoKind.Budget, docx, budget, "Departments", "tom", eng, 5, 134, "Reports"),
            D("FY2026 budget — Marketing", DemoKind.Budget, docx, budget, "Departments", "sofia", marketing, 4, 131, "Reports"),
            D("FY2026 budget — Customer Success", DemoKind.Budget, pdf, budget, "Departments", "amara", cs, 3, 128, "Reports"),
            D("FY2026 budget — Operations", DemoKind.Budget, docx, budget, "Departments", User, null, 5, 126, "Reports"),
            D("Revenue forecast FY2026", DemoKind.Report, pdf, budget, "Forecasts", User, null, 6, 120, "Reports"),
            D("Headcount forecast", DemoKind.Report, docx, budget, "Forecasts", User, null, 4, 117, "Reports"),
            D("Spend by department", DemoKind.BarChart, png, budget, "Forecasts", User, null, 3, 110, "Reports"),
            D("Board pack — Q3 2026", DemoKind.Report, pdf, budget, "Board pack", User, null, 3, 52, "Presentations", "Reports"),
            D("Board pack — Q4 2026", DemoKind.Report, pdf, budget, "Board pack", User, null, 2, 9, "Presentations", "Reports"),
            D("Budget split by team", DemoKind.Donut, png, budget, "Board pack", User, null, 2, 50, "Reports"),

            // ---- Customer Onboarding
            D("Enterprise onboarding playbook", DemoKind.Guide, pdf, onboarding, "Playbooks", "amara", cs, 5, 240, "Manuals"),
            D("SMB onboarding playbook", DemoKind.Guide, docx, onboarding, "Playbooks", "amara", cs, 3, 205, "Manuals"),
            D("Kick-off call agenda template", DemoKind.Guide, md, onboarding, "Templates", User, cs, 2, 180, "Manuals"),
            D("Welcome email templates", DemoKind.Guide, md, onboarding, "Templates", "maya", cs, 4, 170, "Manuals"),
            D("Onboarding survey results — Q3", DemoKind.Report, docx, onboarding, "Feedback", "amara", cs, 2, 44, "Research", "Reports"),
            D("Time to first value", DemoKind.BarChart, png, onboarding, "Feedback", "amara", cs, 3, 40, "Reports"),
            D("Customer feedback log", DemoKind.LogExport, txt, onboarding, "Feedback", User, cs, 3, 90, "Research"),

            // ---- Product Launch — Atlas
            D("Atlas positioning and messaging", DemoKind.Proposal, docx, atlas, "Messaging", "sofia", marketing, 6, 160, "Presentations"),
            D("Atlas launch plan", DemoKind.Proposal, pdf, atlas, null, User, marketing, 5, 150, "Presentations"),
            D("Campaign brief — Atlas", DemoKind.Proposal, docx, atlas, "Campaign", "sofia", marketing, 3, 120, "Presentations"),
            D("Launch email sequence", DemoKind.Guide, md, atlas, "Campaign", "sofia", marketing, 4, 100, "Manuals"),
            D("Campaign results — week 1", DemoKind.BarChart, png, atlas, "Campaign", "sofia", marketing, 2, 24, "Reports"),
            D("Atlas landing page wireframe", DemoKind.Wireframe, png, atlas, "Campaign/Assets", "elena", marketing, 3, 110, "Design"),
            D("Press release — Atlas launch", DemoKind.Press, docx, atlas, "Press", "sofia", marketing, 4, 35, "Presentations"),
            D("Analyst briefing notes", DemoKind.MeetingNotes, md, atlas, "Press", "sofia", marketing, 2, 30, "Meeting notes"),
            D("Atlas launch retrospective", DemoKind.MeetingNotes, md, atlas, null, User, marketing, 3, 14, "Meeting notes"),

            // ---- Platform Reliability
            D("Postmortem — login outage", DemoKind.Postmortem, md, ops, "Postmortems", "daniel", eng, 4, 145, "Reports"),
            D("Postmortem — delayed exports", DemoKind.Postmortem, md, ops, "Postmortems", "tom", eng, 3, 88, "Reports"),
            D("Postmortem — search latency", DemoKind.Postmortem, docx, ops, "Postmortems", "daniel", eng, 2, 26, "Reports"),
            D("Runbook — database failover", DemoKind.Guide, md, ops, "Runbooks", "daniel", eng, 5, 230, "Manuals"),
            D("Runbook — certificate renewal", DemoKind.Guide, md, ops, "Runbooks", "daniel", eng, 3, 160, "Manuals"),
            D("On-call handbook", DemoKind.Guide, pdf, ops, "Runbooks", "tom", eng, 6, 300, "Manuals", "Policies"),
            D("SLO dashboard snapshot", DemoKind.LineChart, png, ops, "Monitoring", "daniel", eng, 3, 70, "Reports"),
            D("Error budget report — Q3", DemoKind.Report, docx, ops, "Monitoring", User, eng, 2, 48, "Reports"),
            D("API gateway logs — sample", DemoKind.LogExport, txt, ops, "Monitoring", "daniel", eng, 1, 12, "Reports"),
            D("Service map", DemoKind.Diagram, png, ops, "Monitoring", "tom", eng, 4, 200, "Design"),

            // ---- Hiring Q1
            D("Job description — Senior Backend Engineer", DemoKind.JobDescription, docx, hiring, "Job descriptions", User, eng, 3, 75, "Manuals"),
            D("Job description — Product Designer", DemoKind.JobDescription, docx, hiring, "Job descriptions", "maya", product, 2, 70, "Manuals"),
            D("Job description — Customer Success Manager", DemoKind.JobDescription, docx, hiring, "Job descriptions", User, cs, 2, 66, "Manuals"),
            D("Job description — Data Analyst", DemoKind.JobDescription, pdf, hiring, "Job descriptions", User, null, 1, 60, "Manuals"),
            D("Interview kit — Backend", DemoKind.Guide, md, hiring, "Interview kits", "tom", eng, 2, 64, "Manuals"),
            D("Interview kit — Design", DemoKind.Guide, md, hiring, "Interview kits", "elena", product, 2, 62, "Manuals"),
            D("Interview kit — Customer Success", DemoKind.Guide, md, hiring, "Interview kits", "amara", cs, 1, 58, "Manuals"),
            D("Hiring plan Q1", DemoKind.Report, docx, hiring, null, User, null, 3, 80, "Reports"),

            // ---- Office Move 2025 (archived)
            D("Office move plan", DemoKind.Proposal, docx, move, null, User, null, 4, 395, "Presentations"),
            D("Floor plan — level 3", DemoKind.Diagram, png, move, "Floor plans", User, null, 3, 385, "Design"),
            D("Moving company quote", DemoKind.Invoice, pdf, move, "Vendors", User, null, 2, 375, "Invoices"),
            D("Move day checklist", DemoKind.Guide, md, move, null, User, null, 2, 365, "Manuals"),

            // ---- Security Audit 2026 (the demo admin's)
            D("Security audit scope", DemoKind.Proposal, docx, audit, null, Admin, legal, 3, 100, "Policies"),
            D("Findings — access reviews", DemoKind.Report, docx, audit, "Findings", "jonas", legal, 4, 80, "Reports"),
            D("Findings — encryption at rest", DemoKind.Report, pdf, audit, "Findings", Admin, legal, 2, 60, "Reports"),
            D("Penetration test summary", DemoKind.Report, pdf, audit, "Findings", Admin, eng, 2, 45, "Reports"),
            D("Audit evidence index", DemoKind.LogExport, txt, audit, "Evidence", Admin, null, 3, 70, "Reports"),

            // ---- Vendor Management (the demo admin's)
            D("Service agreement — Northwind Hosting", DemoKind.Contract, docx, vendors, "Contracts", "jonas", legal, 7, 330, "Contracts"),
            D("Service agreement — Contoso Analytics", DemoKind.Contract, pdf, vendors, "Contracts", "jonas", legal, 5, 270, "Contracts"),
            D("Service agreement — Fabrikam Design Studio", DemoKind.Contract, docx, vendors, "Contracts", Admin, marketing, 4, 210, "Contracts"),
            D("Data processing agreement — Tailspin Mail", DemoKind.Contract, pdf, vendors, "Contracts", "jonas", eng, 6, 190, "Contracts", "Policies"),
            D("NDA — Adventure Works", DemoKind.Contract, docx, vendors, "Contracts", Admin, null, 3, 120, "Contracts"),

            // ---- Company documents outside projects
            D("Travel and expenses policy", DemoKind.Policy, pdf, null, null, User, marketing, 4, 280, "Policies"),
            D("Remote work policy", DemoKind.Policy, docx, null, null, User, product, 3, 260, "Policies"),
            D("Acceptable use policy", DemoKind.Policy, pdf, null, null, "jonas", eng, 5, 320, "Policies"),
            D("Code of conduct", DemoKind.Policy, docx, null, null, User, null, 2, 340, "Policies"),
            D("Quarterly planning — Q4 notes", DemoKind.MeetingNotes, md, null, null, User, product, 2, 47, "Meeting notes"),
            D("All-hands outline — October", DemoKind.Report, md, null, null, User, null, 2, 34, "Presentations"),
            D("Team offsite agenda", DemoKind.Guide, md, null, null, User, marketing, 3, 22, "Manuals"),
            D("Vendor shortlist — CRM", DemoKind.Research, docx, null, null, "maya", product, 3, 55, "Research"),
            D("Org chart", DemoKind.Diagram, png, null, null, User, null, 2, 150, "Design"),
            D("Support ticket trends", DemoKind.BarChart, png, null, null, "amara", cs, 2, 16, "Reports"),
        };

        // ---- Series: things a team writes again and again.
        var customers = new[] { "Contoso", "Fabrikam", "Tailspin Toys", "Adventure Works", "Litware" };
        for (var i = 0; i < customers.Length; i++)
        {
            docs.Add(D($"Customer check-in — {customers[i]}", DemoKind.MeetingNotes, md, onboarding, "Feedback", "amara", cs, 1 + i % 2, 150 - i * 27, "Meeting notes"));
        }

        var releases = new[] { "2.0.0", "2.0.1", "2.0.2", "2.1.0", "2.1.1", "2.2.0" };
        for (var i = 0; i < releases.Length; i++)
        {
            docs.Add(D($"Release notes — {releases[i]}", DemoKind.ReleaseNotes, md, app, "Releases", "lucas", eng, 1 + (i % 3 == 0 ? 1 : 0), 92 - i * 15, "Manuals"));
        }

        var months = new[] { "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct" };
        for (var i = 0; i < months.Length; i++)
        {
            docs.Add(D($"Monthly KPI report — {months[i]} 2026", DemoKind.Report, i % 2 == 0 ? pdf : docx, budget, null, User, null, 1 + i % 2, 245 - i * 30, "Reports"));
        }

        for (var i = 0; i < 10; i++)
        {
            docs.Add(D($"Invoice INV-2026-{101 + i} — Northwind Hosting", DemoKind.Invoice, pdf, vendors, "Invoices", i % 3 == 2 ? "sam" : "priya", finance, i == 4 ? 2 : 1, 300 - i * 30, "Invoices"));
        }

        // Numbered, not dated: a title must not depend on the day the seed ran (it is the key for rebuilding files).
        for (var i = 0; i < 8; i++)
        {
            docs.Add(D($"Weekly product sync #{48 - i}", DemoKind.MeetingNotes, md, null, null, "maya", product,
                1 + (i % 3 == 1 ? 1 : 0), 4 + i * 14, "Meeting notes"));
        }

        for (var i = 0; i < 6; i++)
        {
            docs.Add(D($"Engineering standup #{112 - i}", DemoKind.MeetingNotes, md, ops, null, "tom", eng, 1, 2 + i * 7, "Meeting notes"));
        }

        return docs;
    }

    /// <summary>The catalogue entry of a seeded document, found by its title.</summary>
    public static DemoDoc? Find(string title) => ByTitle.TryGetValue(title, out var doc) ? doc : null;

    private static readonly Dictionary<string, DemoDoc> ByTitle = Documents.ToDictionary(d => d.Title, StringComparer.Ordinal);

    public static DemoPerson? Person(string key) => Colleagues.FirstOrDefault(p => p.Key == key);
}
