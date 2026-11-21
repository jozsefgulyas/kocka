using System.Drawing.Drawing2D;

namespace DobokockaApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FoAblak());
    }
}

/// <summary>Egy kirajzolt dobókocka vezérlő.</summary>
public class KockaVezerlo : Control
{
    private int _ertek = 1;

    public int Ertek
    {
        get => _ertek;
        set { _ertek = Math.Clamp(value, 1, 6); Invalidate(); }
    }

    public Color KockaSzin { get; set; } = Color.White;
    public Color PottySzin { get; set; } = Color.FromArgb(30, 30, 30);

    public KockaVezerlo()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(100, 100);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int meret = Math.Min(Width, Height) - 8;
        var teglalap = new Rectangle((Width - meret) / 2, (Height - meret) / 2, meret, meret);
        int sugar = meret / 6;

        // Árnyék
        using (var arnyekUt = LekerekitettTeglalap(new Rectangle(teglalap.X + 4, teglalap.Y + 4, meret, meret), sugar))
        using (var arnyekEcset = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            g.FillPath(arnyekEcset, arnyekUt);

        // Kocka test
        using (var ut = LekerekitettTeglalap(teglalap, sugar))
        {
            using (var ecset = new LinearGradientBrush(teglalap, KockaSzin,
                       ControlPaint.Dark(KockaSzin, 0.05f), LinearGradientMode.ForwardDiagonal))
                g.FillPath(ecset, ut);
            using var toll = new Pen(Color.FromArgb(120, 120, 120), 2);
            g.DrawPath(toll, ut);
        }

        // Pöttyök
        float p = meret / 4f;                  // pöttyök közötti távolság
        float r = meret / 10f;                 // pötty sugara
        float cx = teglalap.X + meret / 2f;
        float cy = teglalap.Y + meret / 2f;

        var pontok = Ertek switch
        {
            1 => new[] { (0, 0) },
            2 => new[] { (-1, -1), (1, 1) },
            3 => new[] { (-1, -1), (0, 0), (1, 1) },
            4 => new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) },
            5 => new[] { (-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1) },
            _ => new[] { (-1, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1) },
        };

        using var pottyEcset = new SolidBrush(Ertek == 1 ? Color.Firebrick : PottySzin);
        foreach (var (dx, dy) in pontok)
        {
            float x = cx + dx * p;
            float y = cy + dy * p;
            g.FillEllipse(pottyEcset, x - r, y - r, 2 * r, 2 * r);
        }
    }

    private static GraphicsPath LekerekitettTeglalap(Rectangle t, int r)
    {
        var ut = new GraphicsPath();
        int d = r * 2;
        ut.AddArc(t.X, t.Y, d, d, 180, 90);
        ut.AddArc(t.Right - d, t.Y, d, d, 270, 90);
        ut.AddArc(t.Right - d, t.Bottom - d, d, d, 0, 90);
        ut.AddArc(t.X, t.Bottom - d, d, d, 90, 90);
        ut.CloseFigure();
        return ut;
    }
}

/// <summary>A program főablaka.</summary>
public class FoAblak : Form
{
    private readonly Random _rnd = new();
    private readonly List<KockaVezerlo> _kockak = new();
    private readonly System.Windows.Forms.Timer _animacio = new() { Interval = 60 };
    private int _animacioLepes;
    private int _dobasSzam;
    private readonly int[] _statisztika = new int[7];

    private readonly NumericUpDown _darabszam;
    private readonly Button _dobasGomb;
    private readonly Button _torlesGomb;
    private readonly FlowLayoutPanel _kockaPanel;
    private readonly Label _osszegCimke;
    private readonly ListBox _elozmenyek;
    private readonly Label _statCimke;

    public FoAblak()
    {
        Text = "Dobókocka szimulátor";
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 110, 60);   // zöld "asztal"
        Font = new Font("Segoe UI", 10f);
        KeyPreview = true;

        // --- Felső vezérlősáv ---
        var felso = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(12, 12, 12, 0),
            BackColor = Color.FromArgb(18, 80, 44),
        };

        var cimke = new Label
        {
            Text = "Kockák száma:",
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 6, 6, 0),
        };
        _darabszam = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 2, Width = 60 };
        _darabszam.ValueChanged += (_, _) => KockakLetrehozasa();

        _dobasGomb = new Button
        {
            Text = "🎲 Dobás! (Szóköz)",
            AutoSize = true,
            BackColor = Color.Gold,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(16, 0, 0, 0),
        };
        _dobasGomb.Click += (_, _) => Dobas();

        _torlesGomb = new Button
        {
            Text = "Előzmények törlése",
            AutoSize = true,
            BackColor = Color.WhiteSmoke,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8, 0, 0, 0),
        };
        _torlesGomb.Click += (_, _) => Torles();

        felso.Controls.AddRange(new Control[] { cimke, _darabszam, _dobasGomb, _torlesGomb });

        // --- Jobb oldali előzmények + statisztika ---
        var jobb = new Panel
        {
            Dock = DockStyle.Right,
            Width = 230,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(18, 80, 44),
        };
        var elozmenyCimke = new Label
        {
            Text = "Előzmények",
            Dock = DockStyle.Top,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Height = 26,
        };
        _elozmenyek = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        _statCimke = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            ForeColor = Color.White,
            Font = new Font("Consolas", 9.5f),
            Padding = new Padding(0, 8, 0, 0),
        };
        jobb.Controls.Add(_elozmenyek);
        jobb.Controls.Add(_statCimke);
        jobb.Controls.Add(elozmenyCimke);

        // --- Alsó összeg kijelző ---
        _osszegCimke = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            Text = "Nyomd meg a Dobás gombot!",
        };

        // --- Kockák területe ---
        _kockaPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true,
            WrapContents = true,
        };

        Controls.Add(_kockaPanel);
        Controls.Add(_osszegCimke);
        Controls.Add(jobb);
        Controls.Add(felso);

        _animacio.Tick += AnimacioTick;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space) { Dobas(); e.Handled = true; e.SuppressKeyPress = true; }
        };

        KockakLetrehozasa();
        StatisztikaFrissites();
    }

    private void KockakLetrehozasa()
    {
        _kockaPanel.SuspendLayout();
        _kockaPanel.Controls.Clear();
        foreach (var k in _kockak) k.Dispose();
        _kockak.Clear();

        for (int i = 0; i < (int)_darabszam.Value; i++)
        {
            var kocka = new KockaVezerlo
            {
                Size = new Size(110, 110),
                Margin = new Padding(10),
                Ertek = _rnd.Next(1, 7),
            };
            kocka.Click += (_, _) => Dobas();
            _kockak.Add(kocka);
            _kockaPanel.Controls.Add(kocka);
        }
        _kockaPanel.ResumeLayout();
    }

    private void Dobas()
    {
        if (_animacio.Enabled) return;   // ne lehessen animáció közben újra dobni
        _animacioLepes = 0;
        _dobasGomb.Enabled = false;
        _darabszam.Enabled = false;
        _osszegCimke.Text = "Gurul...";
        _animacio.Start();
    }

    private void AnimacioTick(object? sender, EventArgs e)
    {
        _animacioLepes++;
        foreach (var k in _kockak)
            k.Ertek = _rnd.Next(1, 7);

        // fokozatosan lassul
        _animacio.Interval = 40 + _animacioLepes * 12;

        if (_animacioLepes >= 12)
        {
            _animacio.Stop();
            _animacio.Interval = 60;
            DobasVege();
        }
    }

    private void DobasVege()
    {
        // végső, valódi dobás
        foreach (var k in _kockak)
        {
            k.Ertek = _rnd.Next(1, 7);
            _statisztika[k.Ertek]++;
        }

        int osszeg = _kockak.Sum(k => k.Ertek);
        _dobasSzam++;
        string ertekek = string.Join(" + ", _kockak.Select(k => k.Ertek));

        _osszegCimke.Text = _kockak.Count > 1 ? $"{ertekek} = {osszeg}" : $"Eredmény: {osszeg}";

        _elozmenyek.Items.Insert(0, $"#{_dobasSzam}:  {ertekek}" + (_kockak.Count > 1 ? $" = {osszeg}" : ""));
        StatisztikaFrissites();

        _dobasGomb.Enabled = true;
        _darabszam.Enabled = true;
        _dobasGomb.Focus();
    }

    private void StatisztikaFrissites()
    {
        int ossz = _statisztika.Sum();
        var sorok = new List<string> { "Statisztika (kockánként):" };
        for (int i = 1; i <= 6; i++)
        {
            double szazalek = ossz == 0 ? 0 : 100.0 * _statisztika[i] / ossz;
            string sav = new string('█', (int)Math.Round(szazalek / 4));
            sorok.Add($"{i}: {_statisztika[i],4} {szazalek,5:0.0}% {sav}");
        }
        _statCimke.Text = string.Join(Environment.NewLine, sorok);
    }

    private void Torles()
    {
        _elozmenyek.Items.Clear();
        Array.Clear(_statisztika);
        _dobasSzam = 0;
        _osszegCimke.Text = "Nyomd meg a Dobás gombot!";
        StatisztikaFrissites();
    }
}
