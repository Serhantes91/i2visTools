using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using I2VISTools.ModelClasses;
using I2VISTools.Subclasses;

namespace I2VISTools.Tools
{
    public static class PrnWorker
    {

        public static void GetMarkersByPositionAndType(string filePath)
        {

            var fs = new FileStream(filePath, FileMode.Open); // открываем файловый поток (нашей prn-ки)

            // массивы байтов для дальнейшего преобразования в нужные типы
            var intArr = new byte[4]; // массив для преобразования байтов в целое число (int)
            var int64Arr = new byte[8]; // массив для преобразования байтов в целое число расширенного диапазона (long)
            var longArr = new byte[45]; // массив для хранения 45 байт
            var doubleArr = new byte[72]; // массив для хранения 72 байт (в одном месте их надо прочесть сразу)
            var byteArr = new byte[1];
            var floatArr = new byte[4]; // для преобразования в вещественное число
            // -------

            var A = new byte[4]; // зачем-то создал ещё один четырёхбайтовый массив (скорее всего, осталось из старых вариантов)

            fs.Read(A, 0, 4); // читаем с потока 4 байта (скорее всего эту и строку выше можно было заменить простым изменением байтовой позиции с 0 до 4

            fs.Read(longArr, 0, 40); // считываем теперь 40 байт и запихиваем их в буферный массив longArr
            var xnumx = BitConverter.ToInt64(longArr, 0); // первые 8 байт из этого массива - кол-во узлов по x. преобразуем их в long и записываем в переменную xnumx (не знаю зачем 8 байт, когда 4 было бы достаточно, но так устроен prn)
            var ynumy = BitConverter.ToInt64(longArr, 8); // следующие 8 байт - кол-во узлов по y. Преобразуем в long и в переменную ynumy
            var mnumx = BitConverter.ToInt64(longArr, 16); // следующие 8 байт для mnumx (если често не знаю что за mnumx и mnumy. Обычно они 8 и 4 соответственно. У меня и в матлабовском скрипте жэти переменные нигде не используются)
            var mnumy = BitConverter.ToInt64(longArr, 24); // следующие 8 байт для mnumy
            var marknum = BitConverter.ToInt64(longArr, 32); // следующие 8 байт - кол-во маркеров
            // здесь отдельно обращаю внимание, что все эти байты в 5 верхних строках берутся с массива, а не считываются с потока (с потока мы уже считали 40 байт и теперь распределяем их по переменным)

            fs.Read(doubleArr, 0, 72); // теперь считываем с поток 72 байта и записываем их в буферный массив doubleArr
            var xsize = BitConverter.ToDouble(doubleArr, 0); // первые 8 байт - ширина модели
            var ysize = BitConverter.ToDouble(doubleArr, 8); // следующие 8 байт - высота модели
            var pinit = new double[5]; // создаем массив типа double из 5 элементов
            for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i); // следующие 40 байт идут в массив pinit (по 8 байт на элемент) 
            var gxkoef = BitConverter.ToDouble(doubleArr, 56); // следующие 8 байт - ускорение свободного падения по x
            var gykoef = BitConverter.ToDouble(doubleArr, 64); // следующие 8 байт - ускорение свободного падения по y

            fs.Read(intArr, 0, 4); // продолжаем считывать с потока. теперь считываем 4 байта и записываем в массив для преобразования в int (intArr)
            var rocknum = BitConverter.ToInt32(intArr, 0); // преобразуем эти 4 байта в int и записываем в переменную rocknum. Это кол-во типов пород (если вы новых в init'е не добавляли, то их 39)
            fs.Read(int64Arr, 0, 8); // считываем с потока 8 байт 
            var bondnum = BitConverter.ToInt64(int64Arr, 0); // преобразуем в long и записываем в переменную bondnum (не знаю что это такое, обычно оно 13600, но оно понадобится для смещения байтовой позиции дальше)
            fs.Read(intArr, 0, 4); // считываем с потока 4 байта
            var n1 = BitConverter.ToInt32(intArr, 0); // записываем их в целочисленную переменную n1 (опять же, не знаю что это такое и зачем, нигде не используется. Здесь только чтобы перескочить 4 байта)
            fs.Read(doubleArr, 0, 8); //считываем 8 байт с потока
            var timesum = BitConverter.ToDouble(doubleArr, 0); // преобразуем в double и в переменную timesum. Это текущее время модели (в годах)


            //var cpos = fs.Position + rocknum*8*12 + rocknum*4;
            //fs.Position = cpos;

            //var phiArray = new double[rocknum];
            
            //for (int i = 0; i < rocknum; i++)
            //{
            //    fs.Read(doubleArr, 0, 8);
            //    phiArray[i] = BitConverter.ToDouble(doubleArr, 0);
            //}


            //----------------
            //теперь перескакиваем вот на такую байтовую позицию:
            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);
            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            // далее создаем одномерный массив gx (это массив расстояний на узлах по x)
            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4); // в цикле считываем каждые 4 байта...
                gx[i] = BitConverter.ToSingle(floatArr, 0); // преобразуем в float и заполняем массив gx
            }

            // то же самое с массивом gy, который содержит глубины на узлах по y
            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }

            //-------------
            // теперь снова перескакиваем, уже на такую байтовую позицию:
            var nodenum1 = xnumx * ynumy;
            var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8);
            fs.Position = curpos1;
            //вот с этой позиции начинается перебор непостредственно маркеров


            double tolerance = 1000;
           
            // создаем цикл по всем маркерам
            for (int i = 0; i < marknum; i++)
            {
                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36); // на каждом шаге цикла читаем по 36 байт и записываем их в буферный массив parBytes

                var xcoord = BitConverter.ToSingle(parBytes, 0); //первые 4 байта - x-компонента
                var ycoord = BitConverter.ToSingle(parBytes, 4); //следующие 4 байта - y-компонента
                var tk = BitConverter.ToSingle(parBytes, 8) - 273; //следующие 4 байта - температура
                var dens = BitConverter.ToSingle(parBytes, 12); //след. 4 байта - плотность
                var watCons = BitConverter.ToSingle(parBytes, 16); //след. 4 байта - содержание воды 
                var und1 = BitConverter.ToSingle(parBytes, 20); //след. 4 байта - не знаю
                var und2 = BitConverter.ToSingle(parBytes, 24); //след. 4 байта - не знаю
                var viscosity = BitConverter.ToSingle(parBytes, 28); //след. 4 байта, возмонжно, - вязкость 
                var deformation = BitConverter.ToSingle(parBytes, 32); //след. 4 байта - возможно, относительная деформация 

                var rid = new byte[1];
                fs.Read(rid, 0, 1); // далее, считываем 1 байт
                var rockid = rid[0]; // это Id породы

                var lx1 = 2080000;
                var ly1 = 30000;
                var lx2 = 2160000;
                var ly2 = 140000;

                var rx1 = 2110000;
                var ry1 = 30000;
                var rx2 = 2180000;
                var ry2 = 130000;

                if (ycoord >= ly1 && ycoord <= ly2 && ycoord <= Config.Tools.LinearFunction(xcoord, lx1, ly1, lx2, ly2) &&
                    ycoord >= Config.Tools.LinearFunction(xcoord, rx1, ry1, rx2, ry2) && rockid == 9)
                {
                    fs.Position -= 1;
                    byte tb = 12;
                    byte[] bytes = {tb};
                    fs.Write(bytes, 0, 1 );
                }

            }

            fs.Close();

        }
        
        public static List<Marker> GetMarkersByRocksIdInRange(string filePath, List<int> rockIndexes, CoordIntRectangle area)
        {
            area.kostyl = 0;
            var fs = new FileStream(filePath, FileMode.Open); // открываем файловый поток (нашей prn-ки)

            // массивы байтов для дальнейшего преобразования в нужные типы
            var intArr = new byte[4]; // массив для преобразования байтов в целое число (int)
            var int64Arr = new byte[8]; // массив для преобразования байтов в целое число расширенного диапазона (long)
            var longArr = new byte[45]; // массив для хранения 45 байт
            var doubleArr = new byte[72]; // массив для хранения 72 байт (в одном месте их надо прочесть сразу)
            var byteArr = new byte[1];
            var floatArr = new byte[4]; // для преобразования в вещественное число
            // -------

            var A = new byte[4]; // зачем-то создал ещё один четырёхбайтовый массив (скорее всего, осталось из старых вариантов)

            fs.Read(A, 0, 4); // читаем с потока 4 байта (скорее всего эту и строку выше можно было заменить простым изменением байтовой позиции с 0 до 4

            fs.Read(longArr, 0, 40); // считываем теперь 40 байт и запихиваем их в буферный массив longArr
            var xnumx = BitConverter.ToInt64(longArr, 0); // первые 8 байт из этого массива - кол-во узлов по x. преобразуем их в long и записываем в переменную xnumx (не знаю зачем 8 байт, когда 4 было бы достаточно, но так устроен prn)
            var ynumy = BitConverter.ToInt64(longArr, 8); // следующие 8 байт - кол-во узлов по y. Преобразуем в long и в переменную ynumy
            var mnumx = BitConverter.ToInt64(longArr, 16); // следующие 8 байт для mnumx (если често не знаю что за mnumx и mnumy. Обычно они 8 и 4 соответственно. У меня и в матлабовском скрипте жэти переменные нигде не используются)
            var mnumy = BitConverter.ToInt64(longArr, 24); // следующие 8 байт для mnumy
            var marknum = BitConverter.ToInt64(longArr, 32); // следующие 8 байт - кол-во маркеров
            // здесь отдельно обращаю внимание, что все эти байты в 5 верхних строках берутся с массива, а не считываются с потока (с потока мы уже считали 40 байт и теперь распределяем их по переменным)

            fs.Read(doubleArr, 0, 72); // теперь считываем с поток 72 байта и записываем их в буферный массив doubleArr
            var xsize = BitConverter.ToDouble(doubleArr, 0); // первые 8 байт - ширина модели
            var ysize = BitConverter.ToDouble(doubleArr, 8); // следующие 8 байт - высота модели
            var pinit = new double[5]; // создаем массив типа double из 5 элементов
            for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i); // следующие 40 байт идут в массив pinit (по 8 байт на элемент) 
            var gxkoef = BitConverter.ToDouble(doubleArr, 56); // следующие 8 байт - ускорение свободного падения по x
            var gykoef = BitConverter.ToDouble(doubleArr, 64); // следующие 8 байт - ускорение свободного падения по y

            fs.Read(intArr, 0, 4); // продолжаем считывать с потока. теперь считываем 4 байта и записываем в массив для преобразования в int (intArr)
            var rocknum = BitConverter.ToInt32(intArr, 0); // преобразуем эти 4 байта в int и записываем в переменную rocknum. Это кол-во типов пород (если вы новых в init'е не добавляли, то их 39)
            fs.Read(int64Arr, 0, 8); // считываем с потока 8 байт 
            var bondnum = BitConverter.ToInt64(int64Arr, 0); // преобразуем в long и записываем в переменную bondnum (не знаю что это такое, обычно оно 13600, но оно понадобится для смещения байтовой позиции дальше)
            fs.Read(intArr, 0, 4); // считываем с потока 4 байта
            var n1 = BitConverter.ToInt32(intArr, 0); // записываем их в целочисленную переменную n1 (опять же, не знаю что это такое и зачем, нигде не используется. Здесь только чтобы перескочить 4 байта)
            fs.Read(doubleArr, 0, 8); //считываем 8 байт с потока
            var timesum = BitConverter.ToDouble(doubleArr, 0); // преобразуем в double и в переменную timesum. Это текущее время модели (в годах)


            //----------------
            //теперь перескакиваем вот на такую байтовую позицию:
            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);
            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            // далее создаем одномерный массив gx (это массив расстояний на узлах по x)
            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4); // в цикле считываем каждые 4 байта...
                gx[i] = BitConverter.ToSingle(floatArr, 0); // преобразуем в float и заполняем массив gx
            }

            // то же самое с массивом gy, который содержит глубины на узлах по y
            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }

            //-------------
            // теперь снова перескакиваем, уже на такую байтовую позицию:
            var nodenum1 = xnumx * ynumy;
            var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8);
            
            fs.Position = curpos1;
            //вот с этой позиции начинается перебор непостредственно маркеров

            var result = new List<Marker>();

            // создаем цикл по всем маркерам
            for (int i = 0; i < marknum; i++)
            {
                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36); // на каждом шаге цикла читаем по 36 байт и записываем их в буферный массив parBytes

                var xcoord = BitConverter.ToSingle(parBytes, 0); //первые 4 байта - x-компонента
                var ycoord = BitConverter.ToSingle(parBytes, 4); //следующие 4 байта - y-компонента
                var tk = BitConverter.ToSingle(parBytes, 8) - 273; //следующие 4 байта - температура
                var dens = BitConverter.ToSingle(parBytes, 12); //след. 4 байта - плотность
                var watCons = BitConverter.ToSingle(parBytes, 16); //след. 4 байта - содержание воды 
                var und1 = BitConverter.ToSingle(parBytes, 20); //след. 4 байта - не знаю
                var und2 = BitConverter.ToSingle(parBytes, 24); //след. 4 байта - не знаю
                var viscosity = BitConverter.ToSingle(parBytes, 28); //след. 4 байта, возмонжно, - вязкость 
                var deformation = BitConverter.ToSingle(parBytes, 32); //след. 4 байта - возможно, относительная деформация 

                var rid = new byte[1];
                fs.Read(rid, 0, 1); // далее, считываем 1 байт
                var rockid = rid[0]; // это Id породы

                if (!area.IsPointWithin(xcoord/1000d, ycoord/1000d)) continue; //если точка за пределами области - пропускаем шаг
                area.kostyl++;
                //todo удалить костыль + возможно имеет смысл поменять местами верхний и нижний ифы
                if (!rockIndexes.Contains(rockid)) continue; // если маркер не того индекса - пропускаем шаг

                result.Add(new Marker
                {
                    XPosition = xcoord,
                    YPosition = ycoord,
                    Temperature = tk,
                    Density = dens,
                    WaterCons = watCons,
                    UndefinedPar1 = und1,
                    UndefinedPar2 = und2,
                    Viscosity = viscosity,
                    Deformation = deformation,
                    Id = (uint)i,
                    RockId = rockid,
                    Age = timesum
                });
            }
            
            fs.Close();

            return result;

        }

        /// <summary>
        /// Заменить ID породы во всех заданных маркерах
        /// </summary>
        /// <param name="filename">Путь входного prn</param>
        /// <param name="markerIndexes">Список индексов маркеров </param>
        /// <param name="replacedRockId">ID породы, на которую меняем входные маркеры</param>
        public static void ChangeMarkersRockId(string filename, List<uint> markerIndexes, byte replacedRockId)
        {
            using (Stream fs = File.Open(filename, FileMode.Open))
            {
                var intArr = new byte[4];
                var int64Arr = new byte[8];
                var longArr = new byte[45];
                var doubleArr = new byte[72];
                var byteArr = new byte[1];
                var floatArr = new byte[4];

                var A = new byte[4];
                fs.Read(A, 0, 4);

                fs.Read(longArr, 0, 40);
                var xnumx = BitConverter.ToInt64(longArr, 0);
                var ynumy = BitConverter.ToInt64(longArr, 8);
                var mnumx = BitConverter.ToInt64(longArr, 16);
                var mnumy = BitConverter.ToInt64(longArr, 24);
                var marknum = BitConverter.ToInt64(longArr, 32);

                // if (ind > marknum) return null; todo вернуть

                fs.Read(doubleArr, 0, 72);
                var xsize = BitConverter.ToDouble(doubleArr, 0);
                var ysize = BitConverter.ToDouble(doubleArr, 8);
                var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
                var gxkoef = BitConverter.ToDouble(doubleArr, 56);
                var gykoef = BitConverter.ToDouble(doubleArr, 64);

                fs.Read(intArr, 0, 4);
                var rocknum = BitConverter.ToInt32(intArr, 0);
                fs.Read(int64Arr, 0, 8);
                var bondnum = BitConverter.ToInt64(int64Arr, 0);
                fs.Read(intArr, 0, 4);
                var n1 = BitConverter.ToInt32(intArr, 0);
                fs.Read(doubleArr, 0, 8);
                var timesum = BitConverter.ToDouble(doubleArr, 0);


                foreach (var ind in markerIndexes)
                {
                    var nodenum1 = xnumx * ynumy;
                    var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind * 37;
                    fs.Position = curpos1;
                    /*
                    var parBytes = new byte[36];
                    fs.Read(parBytes, 0, 36);

                    var rid = new byte[1];
                    fs.Read(rid, 0, 1);

                    fs.Position -= 1;
                
                    */
                    byte tb = replacedRockId;
                    byte[] bytes = { tb };
                    fs.Write(bytes, 0, 1);

                }

            }

            

        }

        public static List<Marker> GetMarkersFromPrnByIndexes(string filePath, List<uint> inds, bool calculatePressure = true)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var intArr = new byte[4];
            var int64Arr = new byte[8];
            var longArr = new byte[45];
            var doubleArr = new byte[72];
            var byteArr = new byte[1];
            var floatArr = new byte[4];

            var A = new byte[4];
            fs.Read(A, 0, 4);

            fs.Read(longArr, 0, 40);
            var xnumx = BitConverter.ToInt64(longArr, 0);
            var ynumy = BitConverter.ToInt64(longArr, 8);
            var mnumx = BitConverter.ToInt64(longArr, 16);
            var mnumy = BitConverter.ToInt64(longArr, 24);
            var marknum = BitConverter.ToInt64(longArr, 32);

            // if (ind > marknum) return null; todo вернуть

            fs.Read(doubleArr, 0, 72);
            var xsize = BitConverter.ToDouble(doubleArr, 0);
            var ysize = BitConverter.ToDouble(doubleArr, 8);
            var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
            var gxkoef = BitConverter.ToDouble(doubleArr, 56);
            var gykoef = BitConverter.ToDouble(doubleArr, 64);

            fs.Read(intArr, 0, 4);
            var rocknum = BitConverter.ToInt32(intArr, 0);
            fs.Read(int64Arr, 0, 8);
            var bondnum = BitConverter.ToInt64(int64Arr, 0);
            fs.Read(intArr, 0, 4);
            var n1 = BitConverter.ToInt32(intArr, 0);
            fs.Read(doubleArr, 0, 8);
            var timesum = BitConverter.ToDouble(doubleArr, 0);


            // -------

            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);

            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4);
                gx[i] = BitConverter.ToSingle(floatArr, 0);
            }

            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }
            //-------

            var result = new List<Marker>();

            foreach (var ind in inds)
            {
                var nodenum1 = xnumx * ynumy;
                var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind * 37;
                fs.Position = curpos1;

                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36);

                var rid = new byte[1];
                fs.Read(rid, 0, 1);

                var resultMarker = new Marker
                {
                    XPosition = BitConverter.ToSingle(parBytes, 0),
                    YPosition = BitConverter.ToSingle(parBytes, 4),
                    Temperature = BitConverter.ToSingle(parBytes, 8) - 273,
                    Density = BitConverter.ToSingle(parBytes, 12),
                    WaterCons = BitConverter.ToSingle(parBytes, 16),
                    UndefinedPar1 = BitConverter.ToSingle(parBytes, 20),
                    UndefinedPar2 = BitConverter.ToSingle(parBytes, 24),
                    Viscosity = BitConverter.ToSingle(parBytes, 28),
                    Deformation = BitConverter.ToSingle(parBytes, 32),
                    RockId = rid[0],
                    Id = ind,
                    Age = timesum
                };

                if (calculatePressure)
                {
                    //     p1--------p3
                    //     |         |
                    //     p2--------p4

                    var p1X = LeftIndex(gx, resultMarker.XPosition);
                    var p1Y = LeftIndex(gy, resultMarker.YPosition);
                    var p2X = p1X;
                    var p2Y = p1Y + 1;
                    var p3X = p1X + 1;
                    var p3Y = p1Y;
                    var p4X = p1X + 1;
                    var p4Y = p1Y + 1;

                    double pressure;

                    if (p1X < 0 || p1Y < 0)
                    {
                        pressure = 0;
                        resultMarker.Pressure = pressure;
                        result.Add(resultMarker);
                        continue;
                    }

                    fs.Position = curpos0 + 120 * ynumy * p1X + 120 * p1Y;
                    fs.Read(floatArr, 0, 4);
                    var v1 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p2X + 120 * p2Y;
                    fs.Read(floatArr, 0, 4);
                    var v2 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p3X + 120 * p3Y;
                    fs.Read(floatArr, 0, 4);
                    var v3 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p4X + 120 * p4Y;
                    fs.Read(floatArr, 0, 4);
                    var v4 = BitConverter.ToSingle(floatArr, 0);

                    var a1X = gx[p1X];
                    var a1Y = gy[p1Y];
                    var a4X = gx[p4X];
                    var a4Y = gy[p4Y];

                    var interpolation = new InterpolationRectangle(new ModPoint(a1X, a1Y), new ModPoint(a4X, a4Y), v1, v2, v3, v4, new ModPoint(resultMarker.XPosition, resultMarker.YPosition));
                    pressure = interpolation.InterpolatedValue;

                    resultMarker.Pressure = pressure;
                }

                result.Add(resultMarker);
            }

            fs.Close();

            return result;
        }


        public static Dictionary<uint, Marker> GetMarkersFromPrnByIndexesDict(string filePath, List<uint> inds, bool calculatePressure = true)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var intArr = new byte[4];
            var int64Arr = new byte[8];
            var longArr = new byte[45];
            var doubleArr = new byte[72];
            var byteArr = new byte[1];
            var floatArr = new byte[4];

            var A = new byte[4];
            fs.Read(A, 0, 4);

            fs.Read(longArr, 0, 40);
            var xnumx = BitConverter.ToInt64(longArr, 0);
            var ynumy = BitConverter.ToInt64(longArr, 8);
            var mnumx = BitConverter.ToInt64(longArr, 16);
            var mnumy = BitConverter.ToInt64(longArr, 24);
            var marknum = BitConverter.ToInt64(longArr, 32);

            // if (ind > marknum) return null; todo вернуть

            fs.Read(doubleArr, 0, 72);
            var xsize = BitConverter.ToDouble(doubleArr, 0);
            var ysize = BitConverter.ToDouble(doubleArr, 8);
            var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
            var gxkoef = BitConverter.ToDouble(doubleArr, 56);
            var gykoef = BitConverter.ToDouble(doubleArr, 64);

            fs.Read(intArr, 0, 4);
            var rocknum = BitConverter.ToInt32(intArr, 0);
            fs.Read(int64Arr, 0, 8);
            var bondnum = BitConverter.ToInt64(int64Arr, 0);
            fs.Read(intArr, 0, 4);
            var n1 = BitConverter.ToInt32(intArr, 0);
            fs.Read(doubleArr, 0, 8);
            var timesum = BitConverter.ToDouble(doubleArr, 0);


            // -------

            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);

            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4);
                gx[i] = BitConverter.ToSingle(floatArr, 0);
            }

            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }
            //-------

            var result = new Dictionary<uint, Marker>();

            foreach (var ind in inds)
            {
                var nodenum1 = xnumx * ynumy;
                var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind * 37;
                fs.Position = curpos1;

                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36);

                var rid = new byte[1];
                fs.Read(rid, 0, 1);

                var resultMarker = new Marker
                {
                    XPosition = BitConverter.ToSingle(parBytes, 0),
                    YPosition = BitConverter.ToSingle(parBytes, 4),
                    Temperature = BitConverter.ToSingle(parBytes, 8) - 273,
                    Density = BitConverter.ToSingle(parBytes, 12),
                    WaterCons = BitConverter.ToSingle(parBytes, 16),
                    UndefinedPar1 = BitConverter.ToSingle(parBytes, 20),
                    UndefinedPar2 = BitConverter.ToSingle(parBytes, 24),
                    Viscosity = BitConverter.ToSingle(parBytes, 28),
                    Deformation = BitConverter.ToSingle(parBytes, 32),
                    RockId = rid[0],
                    Id = ind,
                    Age = timesum
                };

                if (calculatePressure)
                {
                    //     p1--------p3
                    //     |         |
                    //     p2--------p4

                    var p1X = LeftIndex(gx, resultMarker.XPosition);
                    var p1Y = LeftIndex(gy, resultMarker.YPosition);
                    var p2X = p1X;
                    var p2Y = p1Y + 1;
                    var p3X = p1X + 1;
                    var p3Y = p1Y;
                    var p4X = p1X + 1;
                    var p4Y = p1Y + 1;

                    double pressure;

                    if (p1X < 0 || p1Y < 0)
                    {
                        pressure = 0;
                        resultMarker.Pressure = pressure;
                        result.Add(resultMarker.Id, resultMarker);
                        continue;
                    }

                    fs.Position = curpos0 + 120 * ynumy * p1X + 120 * p1Y;
                    fs.Read(floatArr, 0, 4);
                    var v1 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p2X + 120 * p2Y;
                    fs.Read(floatArr, 0, 4);
                    var v2 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p3X + 120 * p3Y;
                    fs.Read(floatArr, 0, 4);
                    var v3 = BitConverter.ToSingle(floatArr, 0);

                    fs.Position = curpos0 + 120 * ynumy * p4X + 120 * p4Y;
                    fs.Read(floatArr, 0, 4);
                    var v4 = BitConverter.ToSingle(floatArr, 0);

                    var a1X = gx[p1X];
                    var a1Y = gy[p1Y];
                    var a4X = gx[p4X];
                    var a4Y = gy[p4Y];

                    var interpolation = new InterpolationRectangle(new ModPoint(a1X, a1Y), new ModPoint(a4X, a4Y), v1, v2, v3, v4, new ModPoint(resultMarker.XPosition, resultMarker.YPosition));
                    pressure = interpolation.InterpolatedValue;

                    resultMarker.Pressure = pressure;
                }

                result.Add(resultMarker.Id, resultMarker);
            }

            fs.Close();

            return result;
        }

        public static void GetTxtfromPrn(string inputPrnPath, string outputTxtPath = null) 
        {
            if (string.IsNullOrWhiteSpace(outputTxtPath)) outputTxtPath = Path.GetDirectoryName(inputPrnPath);
            var outTxt = Regex.Replace( Path.GetFileNameWithoutExtension(inputPrnPath) , @"[\d-]", string.Empty);
            var outTxtC = string.Format("{0}c{1}.txt", outTxt, Regex.Replace(Path.GetFileNameWithoutExtension(inputPrnPath), @"[^0-9]+", ""));
            var outTxtT = string.Format("{0}t{1}.txt", outTxt, Regex.Replace(Path.GetFileNameWithoutExtension(inputPrnPath), @"[^0-9]+", ""));
            /*
            using (var fs = new FileStream(inputPrnPath, FileMode.Open))
            {
                var intArr = new byte[4];
                var int64Arr = new byte[8];
                var longArr = new byte[45];
                var doubleArr = new byte[72];
                var byteArr = new byte[1];
                var floatArr = new byte[4];

                var A = new byte[4];
                fs.Read(A, 0, 4);

                fs.Read(longArr, 0, 40);
                var xnumx = BitConverter.ToInt64(longArr, 0);
                var ynumy = BitConverter.ToInt64(longArr, 8);
                var mnumx = BitConverter.ToInt64(longArr, 16);
                var mnumy = BitConverter.ToInt64(longArr, 24);
                var marknum = BitConverter.ToInt64(longArr, 32);
                
                fs.Read(doubleArr, 0, 72);
                var xsize = BitConverter.ToDouble(doubleArr, 0);
                var ysize = BitConverter.ToDouble(doubleArr, 8);
                var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
                var gxkoef = BitConverter.ToDouble(doubleArr, 56);
                var gykoef = BitConverter.ToDouble(doubleArr, 64);

                fs.Read(intArr, 0, 4);
                var rocknum = BitConverter.ToInt32(intArr, 0);
                fs.Read(int64Arr, 0, 8);
                var bondnum = BitConverter.ToInt64(int64Arr, 0);
                fs.Read(intArr, 0, 4);
                var n1 = BitConverter.ToInt32(intArr, 0);
                fs.Read(doubleArr, 0, 8);
                var timesum = BitConverter.ToDouble(doubleArr, 0);
                
                var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);

                fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

                var gx = new float[xnumx];
                for (int i = 0; i < xnumx; i++)
                {
                    fs.Read(floatArr, 0, 4);
                    gx[i] = BitConverter.ToSingle(floatArr, 0);
                }

                var gy = new float[ynumy];
                for (int i = 0; i < ynumy; i++)
                {
                    fs.Read(floatArr, 0, 4);
                    gy[i] = BitConverter.ToSingle(floatArr, 0);
                }
                
                var result = new List<Marker>();

                foreach (var ind in inds)
                {
                    var nodenum1 = xnumx * ynumy;
                    var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind * 37;
                    fs.Position = curpos1;

                    var parBytes = new byte[36];
                    fs.Read(parBytes, 0, 36);

                    var rid = new byte[1];
                    fs.Read(rid, 0, 1);

                    var resultMarker = new Marker
                    {
                        XPosition = BitConverter.ToSingle(parBytes, 0),
                        YPosition = BitConverter.ToSingle(parBytes, 4),
                        Temperature = BitConverter.ToSingle(parBytes, 8) - 273,
                        Density = BitConverter.ToSingle(parBytes, 12),
                        WaterCons = BitConverter.ToSingle(parBytes, 16),
                        UndefinedPar1 = BitConverter.ToSingle(parBytes, 20),
                        UndefinedPar2 = BitConverter.ToSingle(parBytes, 24),
                        Viscosity = BitConverter.ToSingle(parBytes, 28),
                        Deformation = BitConverter.ToSingle(parBytes, 32),
                        RockId = rid[0],
                        Id = ind,
                        Age = timesum
                    };

                    result.Add(resultMarker);
                }
            }
            */
        }


        private static int LeftIndex(float[] arr, float coord)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                if (arr[i] <= coord && arr[i + 1] > coord) return i;
            }
            return -1;
        }

    }
}
