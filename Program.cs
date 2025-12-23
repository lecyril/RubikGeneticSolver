using System;
using System.Linq;
using System.Collections.Generic;
using Rubik;

namespace Rubik
{
    internal static class Program
    {
        static void Main()
        {
            // Remplace seed(RND$TIMESEED)
            ExternalFcts.InitRandomSeed();

            // Variables (noms repris du Fortran)
            int i, j, k, ll, two_gen, loc2x2x3, scramble_type, moves_total, twogen_bourrin;
            int[] scramble_sequence;
            int[] cube_solved = new int[54];
            int[] cube_scrambled = new int[54];
            int[] cube = new int[54];
            int[] cube_scrambled_copy = new int[54];
            string[] scramble_notation;
            string[] sequence_notation;
            string[] sol1 = Array.Empty<string>(), sol2 = Array.Empty<string>(), sol3 = Array.Empty<string>();
            string char_buffer;
            int[] best2x2x3loc = new int[12];
            int[] best2x2x3fobj = new int[12];
            int[] best2x2x3fobj_copie = new int[12];
            int[,] best2x2x3elits = new int[12, 30];
            int[,] best_temp = new int[12, 2];

            // paramètres algo génétique
            int taille_pop, long_code, Ttot, generation, nbre_intrus, is_continuing;
            double pm, pc, phi;
            int[,] pop = new int[0, 0];
            int[,] perfo = new int[0, 0];
            int[,] perfo_copie = new int[0, 0];
            int[,,] pop_storage = new int[12, 0, 0];

            // initialisation
            cube_solved = ExternalFcts.InitCube();
            scramble_type = 2; // default

            Console.WriteLine("Genetic rubik's cube solver, version 3.0");
            Console.WriteLine();
            Console.WriteLine("Cyril Castella, 2005-2026");
            Console.WriteLine("https://www.francocube.com");
            Console.WriteLine();
            Console.WriteLine("Enter scramble type: 1 = read from scramble.txt, 2 = random, 3 = User-defined");
            scramble_type = ParseInt(Console.ReadLine(), 2);
            Console.WriteLine();
            Console.WriteLine("Enter number of generations for 2x2x3 search (e.g. 10000): ");
            Ttot = ParseInt(Console.ReadLine(), 1000);

            two_gen = 0;
            twogen_bourrin = 0;

            // scramble
            scramble_sequence = new int[37];

            // If scramble_type == 1, try to read moves from scramble.txt.
            // The file may contain moves separated by spaces on a single line, e.g. "U R' F2 U".
            // Split on any whitespace and convert each token with ExternalFcts.ToNumbers.
            // If the file does not exist, is empty, or an error occurs, fallback to scramble_type = 2 (random).
            if (scramble_type == 1)
            {
                try
                {
                    if (System.IO.File.Exists("scramble.txt"))
                    {
                        var content = System.IO.File.ReadAllText("scramble.txt");
                        var tokens = content.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 0)
                        {
                            // Empty file -> fallback to random
                            scramble_type = 2;
                        }
                        else
                        {
                            scramble_sequence = new int[tokens.Length];
                            for (i = 0; i < tokens.Length; i++)
                            {
                                var token = tokens[i].Trim();
                                scramble_sequence[i] = ExternalFcts.ToNumbers(token);
                            }
                        }
                    }
                    else
                    {
                        // File missing -> fallback to random
                        scramble_type = 2;
                    }
                }
                catch
                {
                    // On any I/O/parsing error fallback to random
                    scramble_type = 2;
                }
            }

            if (scramble_type == 2)
            {
                Console.WriteLine("Enter number of moves of the scramble");
                j = ParseInt(Console.ReadLine(), scramble_sequence.Length);
                scramble_sequence = new int[j];
                for (i = 0; i < j; i++)
                {
                    scramble_sequence[i] = (int)(18 * 0.9999 * ExternalFcts.RandomDouble());
                }
                scramble_sequence = ExternalFcts.TrimSequence(scramble_sequence);
            }

            if (scramble_type == 3)
            {
                Console.WriteLine("Enter number of moves of the scramble");
                j = ParseInt(Console.ReadLine(), scramble_sequence.Length);
                Console.WriteLine("Enter the scramble moves. Only one move (HTM) per line !");
                scramble_sequence = new int[j];
                for (i = 0; i < j; i++)
                {
                    char_buffer = Console.ReadLine()?.Trim() ?? "";
                    scramble_sequence[i] = ExternalFcts.ToNumbers(char_buffer);
                }
            }

            scramble_notation = ExternalFcts.ToNotation(scramble_sequence);
            Console.WriteLine(string.Join("", scramble_notation));

            // Ecriture du scramble dans un fichier results.txt (overwrite)
            System.IO.File.WriteAllText("results.txt", string.Join("", scramble_notation) + "//Scramble"+ Environment.NewLine);

            cube_scrambled = ExternalFcts.DoSequence(cube_solved, scramble_sequence);
            Array.Copy(cube_scrambled, cube_scrambled_copy, 54);

            // --- SOLVING 2x2x3 ---
            is_continuing = 0;
            taille_pop = 51;
            nbre_intrus = 5;
            long_code = 30;
            pm = 0.05;
            pc = 0.8;
            phi = 1.5;

            pop_storage = new int[12, taille_pop, long_code];
            pop = new int[taille_pop, long_code];
            perfo = new int[taille_pop, 2];
            perfo_copie = new int[taille_pop, 2];

            // Allow repeating the full 12-location search if no adequate solution found (mimics FORTRAN GOTO 101)
            bool restartLocations;
            do
            {
                restartLocations = false;

                for (loc2x2x3 = 0; loc2x2x3 < 12; loc2x2x3++)
                {
                    Console.WriteLine();
                    Console.WriteLine($"------------2x2x3 position is : {loc2x2x3}       ----------");

                    Array.Copy(cube_scrambled_copy, cube_scrambled, 54);

                    // rotations initiales selon location
                    for (ll = 0; ll < loc2x2x3 / 3; ll++)
                    {
                        cube_scrambled = ExternalFcts.RotateWholeCube(cube_scrambled);
                        Console.WriteLine("whole cube clockwise rotation around Oz (= y)");
                    }

                    switch (loc2x2x3 % 3)
                    {
                        case 1:
                            cube_scrambled = ExternalFcts.TwistCube(cube_scrambled);
                            Console.WriteLine("whole cube rotation around FUR corner (=xy)");
                            break;
                        case 2:
                            for (i = 0; i < 2; i++)
                            {
                                cube_scrambled = ExternalFcts.RotateWholeCubeX(cube_scrambled);
                                Console.WriteLine("whole cube rotation around LR horizontal axis (=x)");
                            }
                            break;
                    }

                    k = ExternalFcts.Is2gen(cube_scrambled);
                    if (k == 1)
                    {
                        Console.WriteLine("2-gen solve");
                        two_gen = 1;
                    }

                    // génération population initiale
                    for (i = 0; i < taille_pop; i++)
                        for (j = 0; j < long_code; j++)
                            pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);

                    if (is_continuing == 1)
                    {
                        for (i = 0; i < taille_pop; i++)
                            for (j = 0; j < long_code; j++)
                                pop[i, j] = pop_storage[loc2x2x3, i, j];
                    }

                    for (generation = 1; generation <= Ttot; generation++)
                    {
                        if (two_gen == 1) pop = ToTwoGen(pop);

                        // trim sequences
                        for (i = 0; i < taille_pop; i++)
                        {
                            int[] row = new int[long_code];
                            for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                            row = ExternalFcts.TrimSequence(row);
                            for (j = 0; j < long_code; j++) pop[i, j] = row[j];
                        }

                        // evaluation
                        for (i = 0; i < taille_pop; i++)
                        {
                            int[] row = new int[long_code];
                            for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                            var perf = ExternalFcts.FObj(cube_scrambled, cube_solved, row, 3);
                            perfo[i, 0] = perf[0];
                            perfo[i, 1] = perf[1];
                        }

                        // classement population
                        int taillePop = perfo.GetLength(0);
                        int[] perfo0 = new int[taillePop];
                        for (int idx = 0; idx < taillePop; idx++) perfo0[idx] = perfo[idx, 0];
                        int[,] popCopy = ExternalFcts.ClassementPopulation(pop, perfo0);
                        pop = popCopy;
                        perfo = ExternalFcts.ClassementPopulation(perfo, perfo0);

                        if (generation % 100000 == 0)
                        {
                            Console.WriteLine($"Generation / elite/ moy. {generation} {perfo[0,0]} {perfo.Cast<int>().Where((v, idx) => idx % 2 == 0).Sum() / (double)taille_pop}");
                        }

                        // remplacement intrus
                        if (nbre_intrus > 0)
                        {
                            for (i = taille_pop - nbre_intrus; i < taille_pop; i++)
                            {
                                for (j = 0; j < long_code; j++)
                                    pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);
                            }
                        }

                        pop = ExternalFcts.PopMariee(pop, phi, pc, perfo);
                        pop = ExternalFcts.PopMutee(pop, pm);
                    }

                    // stocker elite
                    for (i = 0; i < taille_pop; i++)
                        for (j = 0; j < long_code; j++)
                            pop_storage[loc2x2x3, i, j] = pop[i, j];

                    Console.WriteLine("optimal :");
                    var seqLen = perfo[0, 1];
                    sequence_notation = ExternalFcts.ToNotation(GetRow(pop, 0).Take(seqLen).ToArray());
                    Console.WriteLine(string.Join("", sequence_notation));
                    Console.WriteLine("perfo, # moves, entropy");
                    Console.WriteLine($"{perfo[0,0]} {perfo[0,1]} {(perfo[0,0] + perfo[0,1]) / 10.0}");
                    if ((perfo[0,0] + perfo[0,1]) / 10.0 == 16.0) Console.WriteLine("This 2x2x3 was solved :)");
                    Console.WriteLine();

                    best2x2x3loc[loc2x2x3] = loc2x2x3;
                    for (j = 0; j < long_code; j++) best2x2x3elits[loc2x2x3, j] = pop[0, j];
                    best2x2x3fobj[loc2x2x3] = -perfo[0, 1] - (16 - (perfo[0, 0] + perfo[0, 1]) / 10) * 100;
                }

                // ==== Getting into 2-gen (classement des élites) ====
                // Trier les élites par leur fobj
                best2x2x3fobj_copie = (int[])best2x2x3fobj.Clone();
                best2x2x3elits = ExternalFcts.ClassementPopulation(best2x2x3elits, best2x2x3fobj_copie);

                // Trier les emplacements par fobj
                for (i = 0; i < 12; i++)
                {
                    best_temp[i, 0] = best2x2x3loc[i];
                    best_temp[i, 1] = best2x2x3fobj[i];
                }
                int[] bestTempVals = new int[12];
                for (i = 0; i < 12; i++) bestTempVals[i] = best_temp[i, 1];
                best_temp = ExternalFcts.ClassementPopulation(best_temp, bestTempVals);

                // extraire l'ordre des locations
                for (i = 0; i < 12; i++) best2x2x3loc[i] = best_temp[i, 0];

                // si aucune bonne solution, on relance la recherche sur les 12 locations
                if (best_temp[0, 1] < -20)
                {
                    Console.WriteLine("Adding generations to solve the 2x2x3: no good solution was found.");
                    is_continuing = 1;
                    restartLocations = true;
                }
                else
                {
                    is_continuing = 0;
                    // on continue vers la phase getting into 2-gen
                }

            } while (restartLocations);

            // Affichage des meilleurs résultats de la recherche 2x2x3
            Console.WriteLine("Best 2x2x3 locations : ");
            Console.WriteLine(string.Join(", ", best2x2x3loc));
            Console.WriteLine();
            Console.WriteLine("End of the 2x2x3 search.");
            Console.WriteLine("Alg will now try to go into 2-gen for the best location.");
            Console.WriteLine("Location searched : " + best2x2x3loc[0]);
            Console.WriteLine();

            // On réinitialise cube_scrambled avec scramble, puis setup-rotations, puis best 2x2x3 solution
            loc2x2x3 = best2x2x3loc[0];

            Array.Copy(cube_scrambled_copy, cube_scrambled, 54);
            for (ll = 0; ll < loc2x2x3 / 3; ll++)
            {
                cube_scrambled = ExternalFcts.RotateWholeCube(cube_scrambled);
                Console.WriteLine("whole cube clockwise rotation around Oz (= y)");
                // Écrire dans results.txt comme dans le Fortran original
                System.IO.File.AppendAllText("results.txt", "y" + Environment.NewLine);
            }

            switch (loc2x2x3 % 3)
            {
                case 1:
                    cube_scrambled = ExternalFcts.TwistCube(cube_scrambled);
                    Console.WriteLine("whole cube rotation around FUR corner (=xy)");
                    System.IO.File.AppendAllText("results.txt", "xy" + Environment.NewLine);
                    break;
                case 2:
                    for (i = 0; i < 2; i++)
                    {
                        cube_scrambled = ExternalFcts.RotateWholeCubeX(cube_scrambled);
                        Console.WriteLine("whole cube rotation around LR horizontal axis (=x)");
                        System.IO.File.AppendAllText("results.txt", "x" + Environment.NewLine);
                    }
                    break;
            }

            // appliquer la meilleure élite trouvée pour cette location
            int[] bestEliteSeq = new int[long_code];
            for (j = 0; j < long_code; j++) bestEliteSeq[j] = best2x2x3elits[0, j];
            var bestElitePerf = ExternalFcts.FObj(cube_scrambled, cube_solved, bestEliteSeq, 3);
            int eliteSeqLen = bestElitePerf[1];
            if (eliteSeqLen > 0)
            {
                cube_scrambled = ExternalFcts.DoSequence(cube_scrambled, bestEliteSeq.Take(eliteSeqLen).ToArray());
            }
            moves_total = eliteSeqLen;

            // Print dans le fichier de résultats : sol1
            sol1 = ExternalFcts.ToNotation(bestEliteSeq.Take(eliteSeqLen).ToArray());
            System.IO.File.AppendAllText("results.txt", string.Join("", sol1) + "//2x2x3" + Environment.NewLine);

            // ========== Getting into 2-gen (alg génétique)==========
            taille_pop = 51;
            nbre_intrus = 5;
            long_code = 30;
            Ttot = 10000;
            pm = 0.05;
            pc = 0.8;
            phi = 1.5;
            pop = new int[taille_pop, long_code];
            perfo = new int[taille_pop, 2];
            perfo_copie = new int[taille_pop, 2];

            if (ExternalFcts.Is2gen(cube_scrambled) == 1) Ttot = 10;

            // génération population initiale
            for (i = 0; i < taille_pop; i++)
                for (j = 0; j < long_code; j++)
                    pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);

            bool gettingInto2genDone = false;
            while (!gettingInto2genDone)
            {
                for (generation = 1; generation <= Ttot; generation++)
                {
                    if (two_gen == 1) pop = ToTwoGen(pop);

                    // trim sequences
                    for (i = 0; i < taille_pop; i++)
                    {
                        int[] row = new int[long_code];
                        for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                        row = ExternalFcts.TrimSequence(row);
                        for (j = 0; j < long_code; j++) pop[i, j] = row[j];
                    }

                    // evaluation with type 4 (phase getting into 2-gen)
                    for (i = 0; i < taille_pop; i++)
                    {
                        int[] row = new int[long_code];
                        for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                        var perf = ExternalFcts.FObj(cube_scrambled, cube_solved, row, 4);
                        perfo[i, 0] = perf[0];
                        perfo[i, 1] = perf[1];
                    }

                    // classement
                    int taillePop = perfo.GetLength(0);
                    int[] perfo0 = new int[taillePop];
                    for (int idx = 0; idx < taillePop; idx++) perfo0[idx] = perfo[idx, 0];
                    pop = ExternalFcts.ClassementPopulation(pop, perfo0);
                    perfo = ExternalFcts.ClassementPopulation(perfo, perfo0);

                    if (generation % 5000 == 0)
                    {
                        Console.WriteLine($"Generation / elite/ moy. {generation} {perfo[0,0]} {perfo.Cast<int>().Where((v, idx) => idx % 2 == 0).Sum() / (double)taille_pop}");
                    }

                     // intrusion
                    if (nbre_intrus > 0)
                    {
                        for (i = taille_pop - nbre_intrus; i < taille_pop; i++)
                        {
                            for (j = 0; j < long_code; j++)
                                pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);
                        }
                    }

                    pop = ExternalFcts.PopMariee(pop, phi, pc, perfo);
                    pop = ExternalFcts.PopMutee(pop, pm);
                } // end generations

                // After Ttot generations, check result
                if (perfo[0,1] == 1 && ExternalFcts.Is2gen(cube_scrambled) == 1)
                {
                    Console.WriteLine("Cube was already in the 2-gen group.");
                }

                // Fortran checked sum(perfo(1,:)) == 1700 as success condition
                if ((perfo[0,0] + perfo[0,1]) == 1700)
                {
                    Console.WriteLine("Getting into 2-gen phase is done :)");
                    int seqLen = perfo[0,1];
                    if (seqLen > 0)
                    {
                        var seq = GetRow(pop, 0).Take(seqLen).ToArray();
                        sequence_notation = ExternalFcts.ToNotation(seq);
                        if (seqLen > 1)
                        {
                            cube_scrambled = ExternalFcts.DoSequence(cube_scrambled, seq);
                            moves_total += seqLen;
                            Console.WriteLine("optimal :");
                            Console.WriteLine(string.Join("", sequence_notation));
                            Console.WriteLine("# moves");
                            Console.WriteLine(seqLen);
                        }
                        sol2 = sequence_notation;
                        // Écrire sol2 dans results.txt — les rotations ont déjà été écrites plus haut,
                        // ce qui rend la sortie fidèle au Fortran.
                        System.IO.File.AppendAllText("results.txt", string.Join("", sol2) + "//Get into 2-GEN"+ Environment.NewLine);
                    }
                    gettingInto2genDone = true;
                }
                else
                {
                    Console.WriteLine("Adding 10'000 gen. to solve this part");
                    Ttot += 10000;
                    // generate additional generations by continuing the loop
                }
            } // end getting into 2-gen

            // ========== Solving 2-gen =========
            if (twogen_bourrin > 0)
            {
                Console.WriteLine("calling bourrin");
                ExternalFcts.SolveTwogenBourrin(cube_scrambled, cube_solved);
            }
            else
            {
                // Genetic algorithm for solving 2-gen
                taille_pop = 51;
                nbre_intrus = 5;
                long_code = 30;
                Ttot = 200000;
                pm = 0.05;
                pc = 0.8;
                phi = 1.5;
                pop = new int[taille_pop, long_code];
                perfo = new int[taille_pop, 2];
                perfo_copie = new int[taille_pop, 2];

                two_gen = 1;

                for (i = 0; i < taille_pop; i++)
                    for (j = 0; j < long_code; j++)
                        pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);

                bool solved2gen = false;
                while (!solved2gen)
                {
                    for (generation = 1; generation <= Ttot; generation++)
                    {
                        if (two_gen == 1) pop = ToTwoGen(pop);

                        // trim sequences
                        for (i = 0; i < taille_pop; i++)
                        {
                            int[] row = new int[long_code];
                            for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                            row = ExternalFcts.TrimSequence(row);
                            for (j = 0; j < long_code; j++) pop[i, j] = row[j];
                        }

                        // evaluation with type 2 (entropy based)
                        for (i = 0; i < taille_pop; i++)
                        {
                            int[] row = new int[long_code];
                            for (j = 0; j < long_code; j++) row[j] = pop[i, j];
                            var perf = ExternalFcts.FObj(cube_scrambled, cube_solved, row, 2);
                            perfo[i, 0] = perf[0];
                            perfo[i, 1] = perf[1];
                        }

                        // classement
                        int taillePop = perfo.GetLength(0);
                        int[] perfo0 = new int[taillePop];
                        for (int idx = 0; idx < taillePop; idx++) perfo0[idx] = perfo[idx, 0];
                        pop = ExternalFcts.ClassementPopulation(pop, perfo0);
                        perfo = ExternalFcts.ClassementPopulation(perfo, perfo0);

                        if (generation % 10000 == 0)
                        {
                            Console.WriteLine($"Generation / elite/ moy. {generation} {perfo[0,0]} {perfo.Cast<int>().Where((v, idx) => idx % 2 == 0).Sum() / (double)taille_pop}");
                        }

                        // intrusion
                        if (nbre_intrus > 0)
                        {
                            for (i = taille_pop - nbre_intrus; i < taille_pop; i++)
                            {
                                for (j = 0; j < long_code; j++)
                                    pop[i, j] = (int)(ExternalFcts.RandomDouble() * 0.9999 * 18);
                            }
                        }

                        pop = ExternalFcts.PopMariee(pop, phi, pc, perfo);
                        pop = ExternalFcts.PopMutee(pop, pm);
                    } // end generations

                    // Vérifier condition d'arrêt (Fortran used (perfo(1,1)+perfo(1,2))/10. == 48)
                    if (((perfo[0, 0] + perfo[0, 1]) / 10.0) == 48.0)
                    {
                        Console.WriteLine("optimal :");
                        int seqLen = perfo[0, 1];
                        sequence_notation = ExternalFcts.ToNotation(GetRow(pop, 0).Take(seqLen).ToArray());
                        Console.WriteLine(string.Join("", sequence_notation));

                        Console.WriteLine("fobj / # moves / entropy");
                        Console.WriteLine($"{perfo[0,0]} {perfo[0,1]} {(perfo[0,0] + perfo[0,1]) / 10.0}");
                        moves_total = moves_total + perfo[0, 1];

                        Console.WriteLine($"Whole cube was solved in {moves_total} moves :)");

                        sol3 = sequence_notation;
                        System.IO.File.AppendAllText("results.txt", string.Join("", sol3) + "//2-GEN solve" + Environment.NewLine);
                        solved2gen = true;
                    }
                    else
                    {
                        Console.WriteLine("Adding 200 000 more generations to solve this stage.");
                        // continue the while loop to run another Ttot generations
                    }
                } // end solving 2-gen
            }

            System.IO.File.AppendAllText("results.txt", $"total moves # : {moves_total}{Environment.NewLine}");

            Console.WriteLine("End of the program.");
            Console.ReadLine();
        }

        static int ParseInt(string? s, int def)
        {
            if (int.TryParse(s, out var v)) return v;
            return def;
        }

        static int[] GetRow(int[,] arr, int row)
        {
            int cols = arr.GetLength(1);
            int[] res = new int[cols];
            for (int c = 0; c < cols; c++) res[c] = arr[row, c];
            return res;
        }

        static int[,] ToTwoGen(int[,] pop)
        {
            int rows = pop.GetLength(0);
            int cols = pop.GetLength(1);
            int[,] result = new int[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    //result[i, j] = pop[i, j] % 6; // Limite les mouvements à 2-gen (par exemple U, D, R, L, F, B)
                    result[i,j] = pop[i,j] %2+ (pop[i,j]/6)*6; // Limite les mouvements à 2-gen en gardant le type de mouvement (normal, prime, double)
            return result;
        }
    }
}