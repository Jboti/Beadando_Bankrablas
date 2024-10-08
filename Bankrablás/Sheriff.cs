﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Bankrablás
{
    internal class Sheriff : VarosElem
    {
        public static int hp,damage,arany;
        public static (int, int) sheriffLocation;
        public static List<(int, int)> whiskyLocations;
        public static (int, int) varoshazaLocation;

        public Sheriff() 
        {
            hp = 100;
            damage = r.Next(20,36);
            arany = 0;
            varoshazaLocation = (-1, -1);
            whiskyLocations = new List<(int, int)>();
            
        }

        #region Valtozok
        Dictionary<(int, int), List<List<bool>>> followMezok = new Dictionary<(int, int), List<List<bool>>>();
        public Fold newFold = new Fold();
        Random r = new Random();

        #endregion

        #region Arany
        internal void AranyBegyujt(ref VarosElem[,] varos, int aranyEltolX, int aranyEltolY)
        {
            arany++;
            Lep(ref varos,sheriffLocation.Item1+aranyEltolX,sheriffLocation.Item2+aranyEltolY);
        }
        #endregion

        #region KovetoLepes
        internal void SheriffKovetoLepes(ref VarosElem[,] varos, int sheriffX, int sheriffY, (int, int) cel)
        {
            if (cel == (-1, -1))
            {
                Varos.palyaFelfedezve = true;
                return;
            }
            else
            {
                if (!followMezok.ContainsKey(cel))
                {
                    List<List<bool>> temp = new List<List<bool>>();
                    for (int i = 0; i < varos.GetLength(0); i++)
                    {
                        List<bool> templ = new List<bool>();
                        for (int j = 0; j < varos.GetLength(1); j++)
                            templ.Add(false);
                        temp.Add(templ);
                    }
                    followMezok.Add(cel, temp);
                }


            }
            List<double> tavolsagok = new List<double>();

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                    {
                        if (Varos.validmezoCheck(sheriffX + i, sheriffY + j) && varos[sheriffX + i, sheriffY + j] is Fold)
                        {
                            double tempTav = Varos.TavolsagMeres(sheriffX + i, sheriffY + j, cel.Item1, cel.Item2);
                            tavolsagok.Add(tempTav);
                        }
                        else
                            tavolsagok.Add(1000);
                    }
            int ujsheriffX, ujsheriffY, index;
            bool forceBack = false;
            do
            {
                int jarhatatlan = 0;
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if (i != 0 || j != 0)
                            if (!Varos.validmezoCheck(sheriffX + i, sheriffY + j) || followMezok[cel][sheriffX + i][sheriffY + j] || !(varos[sheriffX + i, sheriffY + j] is Fold))
                                jarhatatlan++;
                if (jarhatatlan == 8)
                    followMezok[cel].ForEach(list => list.ForEach(elem => elem = false));
                index = tavolsagok.IndexOf(tavolsagok.Min());
                ujsheriffX = sheriffX + Varos.defaultIranyok[index].Item1;
                ujsheriffY = sheriffY + Varos.defaultIranyok[index].Item2;
                tavolsagok[index] = 1000;
                if ((tavolsagok.Count() * 1000) == tavolsagok.Sum())
                    forceBack = true;
            }
            while (((followMezok[cel][ujsheriffX][ujsheriffY]) && !forceBack) || !(varos[ujsheriffX, ujsheriffY] is Fold));
            followMezok[cel][ujsheriffX][ujsheriffY] = true;
            Lep(ref varos, ujsheriffX, ujsheriffY);
        }

        #endregion

        #region WhiskyLekezeles
        internal void WhiskyFelszed(ref VarosElem[,] varos, int eltolX, int eltolY)
        {
            hp += Whisky.heal;
            Lep(ref varos, sheriffLocation.Item1 + eltolX, sheriffLocation.Item2 + eltolY);
            whiskyLocations.Remove((sheriffLocation.Item1, sheriffLocation.Item2));
            Varos.ElemGen(1, new Whisky());
        }

        public void WhiskyKeres(ref VarosElem[,] varos)
        {
            var (sheriffX, sheriffY) = sheriffLocation;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (Varos.validmezoCheck(sheriffX + i, sheriffY + j) && varos[sheriffX + i, sheriffY + j] is Whisky)
                    {
                        WhiskyFelszed(ref varos, i, j);
                        return;
                    }
            if (whiskyLocations.Count == 1)
                SheriffKovetoLepes(ref varos, sheriffX, sheriffY, whiskyLocations[0]);
            else
            {
                List<double> tavolsagok = new List<double>();
                for (int i = 0; i < whiskyLocations.Count; i++)
                    tavolsagok.Add(Math.Sqrt(Math.Pow(sheriffX - whiskyLocations[i].Item1, 2) + Math.Pow(sheriffY - whiskyLocations[i].Item2, 2)));

                int index = tavolsagok.IndexOf(tavolsagok.Min());
                SheriffKovetoLepes(ref varos, sheriffX, sheriffY, whiskyLocations[index]);
            }
        }
        #endregion

        #region TamadasFuggvenyek
        internal void BanditaLekezel(ref VarosElem[,] varos, int x, int y)
        {
            int bcount = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (Varos.validmezoCheck(sheriffLocation.Item1 + i, sheriffLocation.Item2 + j)
                        && varos[sheriffLocation.Item1 + i, sheriffLocation.Item2 + j] is Bandita)
                        bcount++;
            if (bcount > 1)
                Menekul(ref varos, x, y);
            else
            {
                if (hp < 50)
                    Menekul(ref varos, x, y);
                else
                    Harcol(ref varos, x, y);
            }

        }

        private void Menekul(ref VarosElem[,] varos, int x, int y)
        {
            int mx = sheriffLocation.Item1 + (-1 * x); int my = sheriffLocation.Item2 + (-1 * y);
            if (Varos.validmezoCheck(mx, my) && varos[mx, my] is Fold)
                Lep(ref varos, mx, my);
            else
                SheriffKovetoLepes(ref varos, sheriffLocation.Item1, sheriffLocation.Item2, Varos.SheriffCel());
        }

        private void Harcol(ref VarosElem[,] varos, int x, int y)
        {
            int bx = sheriffLocation.Item1 + x; int by = sheriffLocation.Item2 + y;
            Bandita b = (Bandita)varos[bx,by];
            HarcolasKirajzolas(b);
            b.hp -= damage;
            if (b.hp <= 0)
            {
                arany += b.arany;
                Bandita.banditaCount--;
                Bandita.banditaLocations.Remove((bx,by));
                varos[bx, by] = newFold;
            }
        }

        private void HarcolasKirajzolas(Bandita b)
        {
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"\tBandita hp: {b.hp}");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($" ( -{damage} )");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(b.hp - damage <= 0 ? " --> 0" : $" --> {b.hp-damage}");
            Console.WriteLine();
            System.Threading.Thread.Sleep(1500);
        }

        #endregion

        #region BasicFuggvenyek
        private void Lep(ref VarosElem[,] varos, int ujx, int ujy)
        {
            var (sheriffX, sheriffY) = sheriffLocation;
            varos[ujx, ujy] = varos[sheriffX, sheriffY];
            sheriffLocation = (ujx, ujy);
            varos[sheriffX, sheriffY] = newFold;
            Varos.SheriffHelyFelforditas(ujx, ujy);
        }

       
        public override string ToString()
        {
            return "S";
        }
        #endregion
    }
}
