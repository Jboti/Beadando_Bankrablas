using System;
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
        Dictionary<(int, int), List<List<bool>>> followMezok = new Dictionary<(int, int), List<List<bool>>>();
        public Fold newFold = new Fold();

        #region SheriffLepesTombok

        //közelben lévő felfedezetlen területekhez és értük teendő lépések indexei
        (int, int)[] sheriffUjmezoCheckIranyok = new (int, int)[] { (-2, 0), (-2, -1), (-2, 1),
                                                                    (0, -2), (-1, -2), (1, -2),
                                                                    (2, 0), (2, -1), (2 , 1),
                                                                    (0, 2), (1, 2), (-1 , 2)};
        (int, int)[] sheriffLepesIranyok = new (int, int)[] { (-1,0), (-1, -1), (-1, 1),
                                                            (0, -1), (-1, -1), (1, -1),
                                                            (1, 0), (1, -1), (1 , 1),
                                                            (0, 1), (1, 1), (-1 , 1)};

        #endregion
        Random r = new Random();

        public Sheriff() 
        {
            hp = 100;
            damage = r.Next(20,36);
            arany = 0;
            varoshazaLocation = (-1, -1);
            whiskyLocations = new List<(int, int)>();
            
        }

        internal void AranyBegyujt(ref VarosElem[,] varos, int aranyEltolX, int aranyEltolY)
        {
            arany++;
            Lep(ref varos,sheriffLocation.Item1+aranyEltolX,sheriffLocation.Item2+aranyEltolY);
        }

        internal void WhiskyFelszed(ref VarosElem[,] varos, int eltolX, int eltolY)
        {
            hp += Whisky.heal;
            Lep(ref varos, sheriffLocation.Item1 + eltolX, sheriffLocation.Item2 + eltolY);
            whiskyLocations.Remove((sheriffLocation.Item1, sheriffLocation.Item2));
            Varos.ElemGen(1, new Whisky());
        }

        internal void Default(ref VarosElem[,] varos, ref bool[,]felfedezettMezok)
        {
            var (sheriffX, sheriffY) = sheriffLocation;
            for (int i = 0; i < sheriffUjmezoCheckIranyok.Count(); i++)
            {
                int tempX = sheriffX + sheriffUjmezoCheckIranyok[i].Item1;
                int tempY = sheriffY + sheriffUjmezoCheckIranyok[i].Item2;
                (int, int) ujHely = (sheriffX + sheriffLepesIranyok[i].Item1, sheriffY + sheriffLepesIranyok[i].Item2);
                if (Varos.validmezoCheck(tempX, tempY) && Varos.validmezoCheck(ujHely.Item1, ujHely.Item2))
                    if(!felfedezettMezok[tempX, tempY] && (varos[ujHely.Item1, ujHely.Item2] is Fold)){
                        followMezok.Clear();
                        Lep(ref varos, ujHely.Item1, ujHely.Item2);
                        return;
                    }
            }
            SheriffKovetoLepes(ref varos, sheriffX, sheriffY, Varos.SheriffCel());
        }

        
        internal void SheriffKovetoLepes(ref VarosElem[,] varos, int sheriffX, int sheriffY, (int, int) cel)
        {
            if (cel == (-1, -1))
            {
                Varos.palyaFelfedezve = true;
                return;
            }
            else{
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
                index = tavolsagok.IndexOf(tavolsagok.Min());
                ujsheriffX = sheriffX + Varos.defaultIranyok[index].Item1;
                ujsheriffY = sheriffY + Varos.defaultIranyok[index].Item2;
                tavolsagok.RemoveAt(index);
                if ((tavolsagok.Count() * 1000) == tavolsagok.Sum())
                    forceBack = true;
            }
            while (((followMezok[cel][ujsheriffX][ujsheriffY]) && !forceBack) || !(varos[ujsheriffX,ujsheriffY] is Fold));
            followMezok[cel][ujsheriffX][ujsheriffY] = true;
            Lep(ref varos, ujsheriffX, ujsheriffY);
        }

        

        public void WhiskyKeres(ref VarosElem[,] varos)
        {
            var (sheriffX, sheriffY) = sheriffLocation;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (Varos.validmezoCheck(sheriffX + i, sheriffY + j) && varos[sheriffX + i, sheriffY + j] is Whisky)
                    {
                        WhiskyFelszed(ref varos,i,j);
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

        internal void BanditaLekezel(ref VarosElem[,] varos, int x,int y)
        {
            int bcount = 0;
            for (int i = -1; i<= 1;i++)
                for (int j = -1; j<= 1;j++)
                    if(Varos.validmezoCheck(sheriffLocation.Item1+i,sheriffLocation.Item2+j) 
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
            b.hp -= damage;
            if (b.hp <= 0)
            {
                arany += b.arany;
                Bandita.banditaCount--;
                Bandita.banditaLocations.Remove((bx,by));
                varos[bx, by] = newFold;
            }
        }


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

    }
}
