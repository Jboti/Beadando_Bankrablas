using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Bankrablás
{
    internal class Bandita : VarosElem
    {
        public int hp,arany;
        public static int banditaCount;
        public static List<(int, int)> banditaLocations = new List<(int, int)>();

        public Bandita()
        {
            hp = 100;
            arany = 0;
        }

        #region Valtozok
        public Fold newFold = new Fold();
        #endregion

        #region TamadasFuggvenyek
        public void Tamad(ref VarosElem[,] varos, int eltolX, int eltolY, int index)
        {
            Random r = new Random();
            (int, int) sheriffLocation = Sheriff.sheriffLocation;
            int dmg = r.Next(4, 16);
            TamadKiir(dmg);
            Sheriff.hp -= dmg;
            if (Sheriff.hp <= 0)
                Varos.jatekVege = true;    
        }

        private void TamadKiir(int dmg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"\tSheriff hp: {Sheriff.hp}");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($" ( -{dmg} )");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(Sheriff.hp - dmg <= 0 ? " --> 0" : $" --> {Sheriff.hp - dmg}");
            Console.WriteLine();
            System.Threading.Thread.Sleep(1500);
        }

        #endregion

        #region Arany
        public void AranyFelszed(ref VarosElem[,] varos, int aranyEltolX, int aranyEltolY, int index)
        {
            arany++;
            var (banditaX,banditaY) = banditaLocations[index];
            Lep(ref varos, banditaX+aranyEltolX,banditaY+aranyEltolY,index);
        }
        #endregion

        #region Default
        public void DefaultLepes(ref VarosElem[,] varos, int index, (int,int)[] defaultIranyok)
        {
            Random r = new Random();
            var (banditaX, banditaY) = banditaLocations[index];
            (int, int) randomIrany;
            int ujbanditaX, ujbanditaY;
            do
            {
                randomIrany = defaultIranyok[r.Next(defaultIranyok.Count())];
                ujbanditaX = banditaX + randomIrany.Item1;
                ujbanditaY = banditaY + randomIrany.Item2;
            } while (!Varos.validmezoCheck(ujbanditaX, ujbanditaY) ||
                    !(varos[ujbanditaX, ujbanditaY] is Fold));
            Lep(ref varos,ujbanditaX,ujbanditaY,index);
        }
        #endregion

        #region BasicFuggvenyek
        private void Lep(ref VarosElem[,] varos, int x, int y,int index) 
        {
            var (banditaX, banditaY) = banditaLocations[index];
            varos[x, y] = varos[banditaX, banditaY];
            banditaLocations[index] = (x, y);
            varos[banditaX, banditaY] = newFold;
        }


        public override string ToString()
        {
            return "B";
        }
        #endregion
    }
}
