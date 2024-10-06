using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Bankrablás
{
    internal class Varos : VarosElem
    {

        static VarosElem[,] varos;
        static bool[,] felfedezettMezok;
        public static bool palyaFelfedezve;

        public Varos()
        {
            VarosGeneralas(25, 100);
        }

        #region Valtozok
        Random r = new Random();
        public static (int, int)[] defaultIranyok = new (int, int)[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };
        public static bool jatekVege;
        Sheriff sher;
        private int futSzam = 0;
        private Fold newFold = new Fold();
        private Barikad newBarikad = new Barikad();
        #endregion

        #region palyaGeneralas

        //feltölti a tömböt sima föld elemekkel, rágenerálja az akadályokat úgy, hogy minden mező elérhető legyen (ha ez nem sikerül akkor újrageneráljuk)
        private void VarosGeneralas(int meret, int akadaly)
        {
            varos = new VarosElem[meret, meret];
            for (int i = 0; i < meret; i++)
                for (int j = 0; j < meret; j++)
                    varos[i, j] = newFold;

            for (int i = 0; i < akadaly; i++)
            {
                var (rSor, rOszlop) = GeneraltVeletlenPozicio();
                varos[rSor, rOszlop] = newBarikad;
            }
            if (Validpalya(meret, akadaly))
                ElemekKiGeneral();
            else
                VarosGeneralas(meret, akadaly);

        }

        int validMezo = 0;
        bool[,] bejartMezok;
        private bool Validpalya(int meret, int akadaly)
        {
            bejartMezok = new bool[meret, meret];
            VarosBejar(0, 0);
            if (meret * meret - akadaly == validMezo)
                return true;
            else
                return false;

        }

        private void VarosBejar(int x, int y)
        {
            if (validmezoCheck(x, y))
            {
                if (!bejartMezok[x, y] && varos[x, y] is Fold)
                {
                    bejartMezok[x, y] = true;
                    validMezo++;
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            if (i != 0 || j != 0)
                                VarosBejar(x + i, y + j);
                }
            }
        }
        #endregion

        #region elemGeneralas


        //a játék "figuráink" kigenerálása, játék megkezdése futtat()-al
        private void ElemekKiGeneral()
        {
            felfedezettMezok = new bool[varos.GetLength(0), varos.GetLength(1)];
            SheriffGen();
            BanditakGen();
            ElemGen(5, new Arany());
            ElemGen(3, new Whisky());
            ElemGen(1, new Varoshaza());
            Futtat();
        }

        private void BanditakGen()
        {
            for (int i = 0; i < 4; i++)
            {
                var (rSor, rOszlop) = GeneraltVeletlenPozicio();
                varos[rSor, rOszlop] = new Bandita();
                Bandita.banditaLocations.Add((rSor, rOszlop));
            }
            Bandita.banditaCount = 4;
        }

        private void SheriffGen()
        {
            var (rSor, rOszlop) = GeneraltVeletlenPozicio();
            varos[rSor, rOszlop] = new Sheriff();
            Sheriff.sheriffLocation = (rSor, rOszlop);
            SheriffHelyFelforditas(rSor, rOszlop);
        }

        public static void ElemGen(int db, VarosElem elem)
        {
            for (int i = 0; i < db; i++)
            {
                var (rSor, rOszlop) = GeneraltVeletlenPozicio();
                varos[rSor, rOszlop] = elem;
                if (elem is Whisky && felfedezettMezok[rSor, rOszlop])
                    Sheriff.whiskyLocations.Add((rSor,rOszlop));
            }
        }
        #endregion

        #region Futtatas

        bool banditaLep = true;

        
        private void Futtat()
        {
            futSzam++;
            Kirajzol();
            if (jatekVege)
            {
                JatekVege();
                return;
            }

            SheriffLepes();
            

            for (int i = 0; i < Bandita.banditaLocations.Count; i++)
            {
                if (banditaLep)
                    BanditaLepes(i,false);
                else
                    BanditaLepes(i,true);
            }
            banditaLep = !banditaLep;


            System.Threading.Thread.Sleep(100);
            Futtat();
        }

        private void JatekVege()
        {
            Console.Clear();
            Kirajzol();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine("");
            Console.WriteLine("\t\t\t\t     JÁTÉK VÉGE");
            if (Sheriff.hp > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\t\t\t\t   Sheriff Nyert!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\t\t\t\t Banditák Nyertek!");
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\t\t\t\tFuttatások száma: {futSzam}");
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------------------------------------------------");
        }

        #endregion

        #region SheriffLepesek

        private void SheriffLepes()
        {
            var (sheriffX, sheriffY) = Sheriff.sheriffLocation;
            SheriffHelyFelforditas(sheriffX, sheriffY);
            sher = (Sheriff)varos[Sheriff.sheriffLocation.Item1, Sheriff.sheriffLocation.Item2];

            if (Sheriff.hp < 50 && Sheriff.whiskyLocations.Count != 0 && Bandita.banditaLocations.Count > 0)
            {
                sher.WhiskyKeres(ref varos);
               
            }
            else if (Sheriff.arany == 5 && Sheriff.varoshazaLocation != (-1, -1))
            {
                SheriffScan(ref varos, sheriffX, sheriffY, true);
                if(!jatekVege)
                    sher.SheriffKovetoLepes(ref varos, sheriffX, sheriffY, Sheriff.varoshazaLocation);
               
            }
            else if (palyaFelfedezve && Sheriff.arany != 5)
            {

                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (validmezoCheck(sheriffX + i, sheriffY + j) && varos[sheriffX + i, sheriffY + j] is Bandita)
                        { 
                            sher.BanditaLekezel(ref varos, i, j);
                            return;
                        }
                
                if(Bandita.banditaLocations.Count > 0)
                    sher.SheriffKovetoLepes(ref varos, sheriffX, sheriffY, Bandita.banditaLocations[0]);
                else
                    SheriffScan(ref varos, sheriffX, sheriffY, false);
            }
            else
                SheriffScan(ref varos, sheriffX, sheriffY, false);
            
            
           
        }

        internal void SheriffScan(ref VarosElem[,] varos, int sheriffX, int sheriffY, bool action)
        {
            
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    int x = sheriffX + i; int y = sheriffY + j;
                    if (validmezoCheck(x, y))
                    {

                        if (varos[sheriffX + i, sheriffY + j] is Varoshaza)
                        {
                            if (Sheriff.arany == 5)
                            {
                                Sheriff.varoshazaLocation = (x, y);
                                jatekVege = true;
                                return;
                            }
                            else
                                Sheriff.varoshazaLocation = (x, y);
                        }

                        if (varos[x, y] is Whisky)
                        {
                            if (Sheriff.hp <= 50 && !action)
                            {
                                sher.WhiskyFelszed(ref varos, i, j);
                                action = true;
                            }
                            else
                                if (!Sheriff.whiskyLocations.Contains((x, y)))
                                Sheriff.whiskyLocations.Add((x, y));
                        }

                        if (varos[x, y] is Arany && !action)
                        {
                            sher.AranyBegyujt(ref varos, i, j);
                            action = true;
                        }

                        if (varos[x, y] is Bandita && !action)
                        {
                            sher.BanditaLekezel(ref varos, i, j);
                            action = true;
                        }
                    }
                }
            if (!action)
                sher.SheriffKovetoLepes(ref varos, sheriffX, sheriffY, SheriffCel());
        }

        public static void SheriffHelyFelforditas(int x, int y)
        {
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if(validmezoCheck(x+i,y+j))
                        felfedezettMezok[x + i, y + j] = true;
        }

        
        public static (int,int) SheriffCel()
        {
          
            (int, int) minHely = (-1, -1);
            double minTav = 10000;
            for (int i = 0; i < felfedezettMezok.GetLength(0); i++)
            {
                for (int j = 0; j < felfedezettMezok.GetLength(1); j++)
                {
                    if (!felfedezettMezok[i, j])
                    {
                        double temptav = TavolsagMeres(Sheriff.sheriffLocation.Item1, Sheriff.sheriffLocation.Item2, i, j);
                        if (temptav < minTav)
                        { 
                            minHely = (i, j);
                            minTav = temptav;
                        }
                    }

                }
                
            }
            
            return minHely;
        }

        #endregion

        #region BanditaLepesek

        private void BanditaLepes(int index, bool tamad)
        {
            var (banditaX, banditaY) = Bandita.banditaLocations[index];
            Bandita bandit = (Bandita)varos[banditaX, banditaY];
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++){
                    if (validmezoCheck(banditaX + i, banditaY + j))
                    {
                        if (varos[banditaX + i, banditaY + j] is Sheriff)
                        {
                            Kirajzol();
                            bandit.Tamad(ref varos, i, j, index);
                            return;
                        }
                        if (varos[banditaX + i, banditaY + j] is Arany && !tamad){
                            bandit.AranyFelszed(ref varos, i, j, index);
                            return;
                        }
                    }
                }
            if(!tamad)
                bandit.DefaultLepes(ref varos, index, defaultIranyok);
        }
        
        #endregion

        #region Kirajzolas

        
        string[] elemek = new string[] { "S","B","A","W","X","V"," "};
        ConsoleColor[] elemSzinek = new ConsoleColor[] { ConsoleColor.Blue,ConsoleColor.DarkRed,ConsoleColor.DarkYellow,ConsoleColor.Magenta,ConsoleColor.DarkGray,ConsoleColor.Green,ConsoleColor.Black};

        private void Kirajzol()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -");
            for (int i = 0; i < varos.GetLength(0); i++)
            {
                Console.Write("|");
                for (int j = 0; j < varos.GetLength(1); j++)
                {
                    //debug
                    //string elem = varos[i, j].ToString();
                    //Console.BackgroundColor = elemSzinek[Array.IndexOf(elemek, elem)];
                    //Console.Write($" {elem} ");

                    if (felfedezettMezok[i, j])
                    {
                        string elem = varos[i, j].ToString();
                        Console.ForegroundColor = elemSzinek[Array.IndexOf(elemek, elem)];
                        Console.Write($" {elem} ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($" * ");
                    }
                    Console.ForegroundColor = ConsoleColor.DarkCyan;

                }
                Console.Write("|");
                Console.WriteLine();
            }
            Console.WriteLine("- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Sheriff hp: {Sheriff.hp} Sheriff damage: {Sheriff.damage}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Banditák száma: {Bandita.banditaLocations.Count}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Sheriff arany: {Sheriff.arany}");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"Whisky: ");
            foreach ((int,int) x in Sheriff.whiskyLocations){
                Console.Write($"({x.Item1},{x.Item2}) ");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            if(Sheriff.varoshazaLocation.Item1 == -1)
                Console.WriteLine("Varoshaza: ");
            else
                Console.WriteLine($"Varoshaza: ({Sheriff.varoshazaLocation.Item1},{Sheriff.varoshazaLocation.Item2})");
            Console.WriteLine();
        }

        #endregion

        #region BasicFunctions

        public static (int, int) GeneraltVeletlenPozicio()
        {
            int rSor, rOszlop;
            Random r = new Random();
            do
            {
                rSor = r.Next(varos.GetLength(0));
                rOszlop = r.Next(varos.GetLength(1));
            } while (!(varos[rSor, rOszlop] is Fold));
            return (rSor, rOszlop);
        }   

        public static bool validmezoCheck(int sor, int oszlop)
        {
            return (sor >= 0 && sor < varos.GetLength(0) && oszlop >= 0 && oszlop < varos.GetLength(1));
        }

        public static double TavolsagMeres(int x1, int y1, int x2, int y2)
        {
            return(Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
        }

        #endregion
    }
}
