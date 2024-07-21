using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Widgets;

/// @author Kaisa-Emilia Korhonen
/// @version 22.04.2020
/// 
/// <summary>
/// Ohjelmointi 1 harjoitustyö, aiheena tasohyppelypeli.
/// </summary>

public class Kalapeli : PhysicsGame
{
    
    private const double NOPEUS = 200;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 40;

    private PlatformCharacter pelaaja;
    private IntMeter laskuri;

    private static readonly Image pelaajanKuva = LoadImage("kala.png");
    private static readonly Image tahtiKuva = LoadImage("tahti.png");
    private static readonly Image vihuKuva = LoadImage( "vihollinen.png");
    private static readonly Image maaliKuva = LoadImage("maali.png");
    private static readonly Image maaKuva = LoadImage(("maa.png"));


    private EasyHighScore pisteLista = new EasyHighScore();
    private List<Label> valikonKohdat;
    private Timer aikaLaskuri = new Timer();
    

    /// <summary>
    /// Ohjelma siirtyy alkuvalikkoon käynnistyttäessä.
    /// </summary>
    public override void Begin()
    {
        AlkuValikko();
    }
    
    
    /// <summary>
    /// Luodaan kenttä ja lisätään oliot. 
    /// </summary>
    private void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta.txt");
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaTahti);
        kentta.SetTileMethod('P', LisaaPelaaja);
        kentta.SetTileMethod('V', LisaaVihollinen);
        kentta.SetTileMethod('M', LisaaMaali);
        kentta.SetTileMethod('L', LisaaMaa);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.CreateGradient(Color.Mint , Color.SkyBlue);
        
    }
    
    
    /// <summary>
    /// Seuraavilla aliohjelmilla luodaan kenttään tarvittavat objektit (tasot, maali, tähdet...).
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.BlueGray;
        Add(taso);
    }
    
    private void LisaaMaa(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject maa = PhysicsObject.CreateStaticObject(leveys, korkeus);
        maa.IgnoresCollisionResponse = true;
        maa.Position = paikka;
        maa.Image = maaKuva;
        maa.Tag = "maa";
        Add(maa);
    }
    
    private void LisaaMaali(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject maali = PhysicsObject.CreateStaticObject(leveys, korkeus);
        maali.IgnoresCollisionResponse = true;
        maali.Position = paikka;
        maali.Image = maaliKuva;
        maali.Tag = "maali";
        Add(maali);
    }
    
    private void LisaaTahti(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Image = tahtiKuva;
        tahti.Tag = "tahti";
        Add(tahti);
    }
    
    
    /// <summary>
    /// Luodaan pelaaja ja sen ominaisuudet kuten massa ja törmäykset
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(leveys, korkeus);
        pelaaja.Position = paikka;
        pelaaja.Mass = 3.0;
        pelaaja.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja, "tahti", TormaaTahteen);
        AddCollisionHandler(pelaaja, "vihollinen", TormaaViholliseen);
        AddCollisionHandler(pelaaja, "maali", PaaseMaaliin);
        AddCollisionHandler(pelaaja, "maa", TipuMaahan);
        Add(pelaaja);
    }
    
    
    /// <summary>
    /// Luodaan viholliset, joiden liikkeiden amplitudi ja taajuus ovat randomeita annetuilla väleillä.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject vihollinen = PhysicsObject.CreateStaticObject(leveys, korkeus);
        vihollinen.IgnoresCollisionResponse = true;
        vihollinen.Position = paikka;
        vihollinen.Image = vihuKuva;
        vihollinen.Tag = "vihollinen";
        vihollinen.Oscillate(Vector.UnitY, RandomGen.NextDouble(80.0, 120.0), RandomGen.NextDouble(0.1, 0.3));
        Add(vihollinen);
    }
    
    
    /// <summary>
    /// Luodaan aika- ja pistelaskurit sekä -näytöt
    /// </summary>
    private void LuoLaskurit()
    {
        laskuri = new IntMeter(0, -5, 100);
        
        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Right - 100;
        pisteNaytto.Y = Screen.Top - 50;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.Transparent;
        pisteNaytto.Title = "Pisteet";
        
        pisteNaytto.BindTo(laskuri);
        Add(pisteNaytto);
        
        aikaLaskuri.Start();

        Label aikaNaytto = new Label();
        aikaNaytto.X = Screen.Right - 200;
        aikaNaytto.Y = Screen.Top - 50;
        aikaNaytto.TextColor = Color.Black;
        aikaNaytto.DecimalPlaces = 1;
        aikaNaytto.BindTo(aikaLaskuri.SecondCounter);
        Add(aikaNaytto);
    }
    
    
    /// <summary>
    /// Lisätään näppäimet, joilla pelaaja liikkuu.
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);
    }
    
    
    /// <summary>
    /// Seuraavilla aliohjelmilla luodaan pelaajan liikkeet.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="nopeus"></param>
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }
    
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }
    
    
    /// <summary>
    /// Aliohjelmat suorittavat törmäykset pelaajan ja kyseisen objektin välille sekä lisää/vähentää pisteet laskurista
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="tahti"></param>
    private void TormaaTahteen(PhysicsObject hahmo, PhysicsObject tahti)
    {
        MessageDisplay.Add("Mahtavaa! +1p");
        tahti.Destroy();
        laskuri.Value += 1;
    }
    
    private void TormaaViholliseen(PhysicsObject hahmo, PhysicsObject vihollinen)
    {
        MessageDisplay.Add( "Auts! -2p");
        vihollinen.Destroy();

        laskuri.Value -= 2;
        if (laskuri.Value < 0)
        {
            laskuri.Value = 0;
            PelaajaKuoli();
        }
    }
    
    private void TipuMaahan(PhysicsObject hahmo, PhysicsObject maa)
    {
        MessageDisplay.Add("O ou!");
        PelaajaKuoli();
    }
    
    
    /// <summary>
    /// Kun pelaaja kuolee tulee näkyviin pistelista ja valikko
    /// </summary>
    private void PelaajaKuoli()
    {
        pelaaja.Destroy();
        aikaLaskuri.Stop();
        pisteLista.EnterAndShow((laskuri.Value));
        pisteLista.HighScoreWindow.Closed += TakaisinValikkoon;
    }
    
    
    /// <summary>
    /// Funktio laskee, kuinka monta pistettä pelaaja kerää sekunnissa ja palauttaa sen arvon.
    /// </summary>
    /// <param name="aika"></param>
    /// <param name="pisteet"></param>
    /// <returns></returns>
    private double PisteitaSekunnissa(double aika, int pisteet)
    {
        double PisteitaSekunnissa;
        PisteitaSekunnissa = pisteet / aika;
        return Math.Round(PisteitaSekunnissa,2);
    }
    
    
    /// <summary>
    ///  Pelaajan päästessä maaliin tuhotaan maali ja kuoletetaan pelaaja. Kutsutaan funktiota PisteitaSekunnissa.
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="maali"></param>
    private void PaaseMaaliin(PhysicsObject hahmo, PhysicsObject maali )
    {
        MessageDisplay.Add("Voitit pelin! Sait " + PisteitaSekunnissa(aikaLaskuri.SecondCounter.Value,laskuri.Value) + " pistettä sekunnissa");
        maali.Destroy();
        
        PelaajaKuoli();
    }
    
    
    /// <summary>
    /// Aloittaa pelin, tyhjentää kentän ja asettaa uudet laskurit, kentän, näppäimet ja muut asetukset.
    /// </summary>
    private void AloitaPeli()
    {
        
        ClearAll();
        aikaLaskuri.Reset();
        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaNappaimet();
        LuoLaskurit();

        Camera.Follow(pelaaja);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
    }
    
    
    /// <summary>
    /// Palataan alkuvalikkoon.
    /// </summary>
    /// <param name="sender"></param>
    private void TakaisinValikkoon(Window sender)
    {
        
        AlkuValikko();
    }
    
    
    /// <summary>
    /// Avataan parhaiden pisteiden lista.
    /// </summary>
    private void ParhaatPisteet()
    {
        pisteLista.Show();
    }
    
    
    /// <summary>
    /// Luodaan alkuvalikko ja käydään läpi sen kolme kohtaa, lisätään hiiren painallukset.
    /// </summary>
    private void AlkuValikko()
    {
        valikonKohdat = new List<Label>();
        
        Label ekaKohta = new Label("Aloita peli");
        ekaKohta.Position = new Vector(0, 40);
        valikonKohdat.Add(ekaKohta);
        
        Label tokaKohta = new Label("Parhaat pisteet");
        tokaKohta.Position = new Vector(0,0);
        valikonKohdat.Add(tokaKohta);
        
        Label kolmasKohta = new Label("Lopeta peli");
        kolmasKohta.Position = new Vector(0, -40);
        valikonKohdat.Add(kolmasKohta);

        foreach (Label valikonKohta in valikonKohdat)
        {
            Add(valikonKohta);
        }
        

        Mouse.ListenOn(ekaKohta, MouseButton.Left, ButtonState.Pressed, AloitaPeli, null);
        Mouse.ListenOn(tokaKohta, MouseButton.Left, ButtonState.Pressed, ParhaatPisteet, null);
        Mouse.ListenOn(kolmasKohta, MouseButton.Left, ButtonState.Pressed, Exit, null);

        Mouse.ListenMovement(1.0, AlkuValikossaLiikkuminen, null);
    }
    
    
    /// <summary>
    /// Liikkuminen valikossa ja värin vaihtaminen hiiren ollessa kohdalla.
    /// </summary>
    private void AlkuValikossaLiikkuminen()
    {
        foreach (Label kohta in valikonKohdat)
        {
            if (Mouse.IsCursorOn(kohta))
            {
                kohta.TextColor = Color.Ruby;
                
                MessageDisplay.TextColor = Color.Black;
            }
            else
            {
                kohta.TextColor = Color.Black;
            }
        }
    }
    
    
}