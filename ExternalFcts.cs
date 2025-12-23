using System;
using System.Linq;
using System.Collections.Generic;

namespace Rubik
{
    internal static class ExternalFcts
    {
        static Random _rng = new Random();

        public static void InitRandomSeed()
        {
            // seed based on current time (different each run)
            unchecked
            {
                _rng = new Random((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
            }
        }

        public static double RandomDouble() => _rng.NextDouble();

        public static int RandomInt(int exclusive) => _rng.Next(exclusive);

        // Init cube: returns array[54] with blocks of 9 having same value 0..5
        public static int[] InitCube()
        {
            var res = new int[54];
            for (int face = 0; face < 6; face++)
                for (int p = 0; p < 9; p++)
                    res[face * 9 + p] = face;
            return res;
        }

        // ToNotation: converts move numbers to a string array like Fortran ToString
        // Each move => three entries: face char, modifier (' ', '2' or '\''), ' '
        public static string[] ToNotation(int[] sequence)
        {
            if (sequence == null) return Array.Empty<string>();
            var outArr = new string[sequence.Length * 3];
            for (int a = 0; a < sequence.Length; a++)
            {
                int move = sequence[a];
                int face = move % 6;
                string faceChar = face switch
                {
                    0 => "F",
                    1 => "U",
                    2 => "R",
                    3 => "B",
                    4 => "D",
                    5 => "L",
                    _ => "?"
                };
                outArr[3 * a + 0] = faceChar;
                if (move > 11) outArr[3 * a + 1] = "'";
                else if (move > 5) outArr[3 * a + 1] = "2";
                else outArr[3 * a + 1] = " ";
                outArr[3 * a + 2] = " ";
            }
            return outArr;
        }

        // ToNumbers: parses string like "F", "F2", "F'" (case-insensitive)
        public static int ToNumbers(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Trim().ToUpperInvariant();
            char c0 = s.Length > 0 ? s[0] : '\0';
            int val = c0 switch
            {
                'F' => 0,
                'U' => 1,
                'R' => 2,
                'B' => 3,
                'D' => 4,
                'L' => 5,
                _ => 0
            };
            if (s.Length > 1)
            {
                if (s[1] == '2') val += 6;
                else if (s[1] == '\'') val += 12;
            }
            return val;
        }

        // DoSequence: apply a sequence of moves to a cube (both zero-based)
        public static int[] DoSequence(int[] cube, int[] moveSequence)
        {
            if (cube == null) throw new ArgumentNullException(nameof(cube));
            var cur = (int[])cube.Clone();
            if (moveSequence == null || moveSequence.Length == 0) return cur;
            foreach (var m in moveSequence)
            {
                cur = DoMove(cur, m);
            }
            return cur;
        }

        // DoMove: applies a single move (including repetitions for 2/3 turns)
        public static int[] DoMove(int[] cubeState, int move)
        {
            var res = (int[])cubeState.Clone();
            int repetition = 0;
            if (move > 5) repetition = 1;
            if (move > 11) repetition = 2;
            if (move > 17) repetition = 3; // full turn (kept for fidelity)

            for (int rep = 0; rep <= repetition; rep++)
            {
                var prev = (int[])res.Clone();
                // rotate face
                res = TurnFace(res, move % 6);

                // rotate ring depending on face
                switch (move % 6)
                {
                    case 0:
                        res[15] = prev[53];
                        res[16] = prev[50];
                        res[17] = prev[47];
                        res[18] = prev[15];
                        res[21] = prev[16];
                        res[24] = prev[17];
                        res[36] = prev[24];
                        res[37] = prev[21];
                        res[38] = prev[18];
                        res[47] = prev[36];
                        res[50] = prev[37];
                        res[53] = prev[38];
                        break;
                    case 1:
                        res[0] = prev[18];
                        res[1] = prev[19];
                        res[2] = prev[20];
                        res[18] = prev[27];
                        res[19] = prev[28];
                        res[20] = prev[29];
                        res[27] = prev[45];
                        res[28] = prev[46];
                        res[29] = prev[47];
                        res[45] = prev[0];
                        res[46] = prev[1];
                        res[47] = prev[2];
                        break;
                    case 2:
                        res[2] = prev[38];
                        res[5] = prev[41];
                        res[8] = prev[44];
                        res[11] = prev[2];
                        res[14] = prev[5];
                        res[17] = prev[8];
                        res[27] = prev[17];
                        res[30] = prev[14];
                        res[33] = prev[11];
                        res[38] = prev[33];
                        res[41] = prev[30];
                        res[44] = prev[27];
                        break;
                    case 3:
                        res[9] = prev[20];
                        res[10] = prev[23];
                        res[11] = prev[26];
                        res[20] = prev[44];
                        res[23] = prev[43];
                        res[26] = prev[42];
                        res[42] = prev[45];
                        res[43] = prev[48];
                        res[44] = prev[51];
                        res[45] = prev[11];
                        res[48] = prev[10];
                        res[51] = prev[9];
                        break;
                    case 4:
                        res[6] = prev[51];
                        res[7] = prev[52];
                        res[8] = prev[53];
                        res[24] = prev[6];
                        res[25] = prev[7];
                        res[26] = prev[8];
                        res[33] = prev[24];
                        res[34] = prev[25];
                        res[35] = prev[26];
                        res[51] = prev[33];
                        res[52] = prev[34];
                        res[53] = prev[35];
                        break;
                    case 5:
                        res[0] = prev[9];
                        res[3] = prev[12];
                        res[6] = prev[15];
                        res[9] = prev[35];
                        res[12] = prev[32];
                        res[15] = prev[29];
                        res[29] = prev[42];
                        res[32] = prev[39];
                        res[35] = prev[36];
                        res[36] = prev[0];
                        res[39] = prev[3];
                        res[42] = prev[6];
                        break;
                    default:
                        // move error - keep res as previous
                        break;
                }
            }

            return res;
        }

        // TurnFace rotates a face 3x3 clockwise
        static int[] TurnFace(int[] cube, int face)
        {
            var res = (int[])cube.Clone();
            int offset = face * 9;
            // mapping as in Fortran (mod used for safety)
            res[offset + 0] = cube[offset + 6];
            res[offset + 1] = cube[offset + 3];
            res[offset + 2] = cube[offset + 0];
            res[offset + 3] = cube[offset + 7];
            res[offset + 5] = cube[offset + 1];
            res[offset + 6] = cube[offset + 8];
            res[offset + 7] = cube[offset + 5];
            res[offset + 8] = cube[offset + 2];
            return res;
        }

        public static int Compare(int[] cube1, int[] cube2)
        {
            int cmp = 0;
            for (int m = 0; m < 54; m++)
            {
                if (cube1[m] == cube2[m]) cmp++;
                if (cube2[m] > 5)
                {
                    if (cube1[m] < 6) cmp++;
                }
            }
            return cmp;
        }

        public static int Entropy(int[] cube)
        {
            int ent = 0;
            // Corner-edge pairs (kept faithful to Fortran)
            if (cube[0] == cube[1] && cube[15] == cube[16]) ent++;
            if (cube[0] == cube[3] && cube[47] == cube[50]) ent++;
            if (cube[47] == cube[46] && cube[15] == cube[12]) ent++;
            if (cube[2] == cube[5] && cube[18] == cube[21]) ent++;
            if (cube[2] == cube[1] && cube[17] == cube[16]) ent++;
            if (cube[17] == cube[14] && cube[18] == cube[19]) ent++;
            if (cube[8] == cube[5] && cube[24] == cube[21]) ent++;
            if (cube[8] == cube[7] && cube[38] == cube[37]) ent++;
            if (cube[38] == cube[41] && cube[24] == cube[25]) ent++;
            if (cube[6] == cube[3] && cube[53] == cube[50]) ent++;
            if (cube[6] == cube[7] && cube[36] == cube[37]) ent++;
            if (cube[36] == cube[39] && cube[53] == cube[52]) ent++;
            if (cube[29] == cube[32] && cube[45] == cube[48]) ent++;
            if (cube[29] == cube[28] && cube[9] == cube[10]) ent++;
            if (cube[9] == cube[12] && cube[45] == cube[46]) ent++;
            if (cube[27] == cube[28] && cube[11] == cube[10]) ent++;
            if (cube[27] == cube[30] && cube[20] == cube[23]) ent++;
            if (cube[11] == cube[14] && cube[20] == cube[19]) ent++;
            if (cube[33] == cube[34] && cube[44] == cube[43]) ent++;
            if (cube[33] == cube[30] && cube[26] == cube[23]) ent++;
            if (cube[44] == cube[41] && cube[26] == cube[25]) ent++;
            if (cube[35] == cube[34] && cube[42] == cube[43]) ent++;
            if (cube[35] == cube[32] && cube[51] == cube[48]) ent++;
            if (cube[42] == cube[39] && cube[51] == cube[52]) ent++;

            // Edges-centers pairs
            if (cube[4] == cube[1]) ent++;
            if (cube[4] == cube[3]) ent++;
            if (cube[4] == cube[5]) ent++;
            if (cube[4] == cube[7]) ent++;
            if (cube[13] == cube[10]) ent++;
            if (cube[13] == cube[12]) ent++;
            if (cube[13] == cube[14]) ent++;
            if (cube[13] == cube[16]) ent++;
            if (cube[22] == cube[19]) ent++;
            if (cube[22] == cube[21]) ent++;
            if (cube[22] == cube[23]) ent++;
            if (cube[22] == cube[25]) ent++;
            if (cube[31] == cube[28]) ent++;
            if (cube[31] == cube[30]) ent++;
            if (cube[31] == cube[32]) ent++;
            if (cube[31] == cube[34]) ent++;
            if (cube[40] == cube[37]) ent++;
            if (cube[40] == cube[39]) ent++;
            if (cube[40] == cube[41]) ent++;
            if (cube[40] == cube[43]) ent++;
            if (cube[49] == cube[46]) ent++;
            if (cube[49] == cube[48]) ent++;
            if (cube[49] == cube[50]) ent++;
            if (cube[49] == cube[52]) ent++;

            return ent;
        }

        public static int Is2x2x3(int[] cube)
        {
            int ok = 0;
            if (cube[33] == cube[34] && cube[44] == cube[43]) ok++;
            if (cube[33] == cube[30] && cube[26] == cube[23]) ok++;
            if (cube[44] == cube[41] && cube[26] == cube[25]) ok++;

            if (cube[35] == cube[34] && cube[42] == cube[43]) ok++;
            if (cube[35] == cube[32] && cube[51] == cube[48]) ok++;
            if (cube[42] == cube[39] && cube[51] == cube[52]) ok++;

            // edges-centers
            if (cube[22] == cube[23]) ok++;
            if (cube[22] == cube[25]) ok++;
            if (cube[31] == cube[30]) ok++;
            if (cube[31] == cube[32]) ok++;
            if (cube[31] == cube[34]) ok++;
            if (cube[40] == cube[39]) ok++;
            if (cube[40] == cube[41]) ok++;
            if (cube[40] == cube[43]) ok++;
            if (cube[49] == cube[48]) ok++;
            if (cube[49] == cube[52]) ok++;

            return ok;
        }

        // Is2gen: faithful translation of the Fortran logic
        public static int Is2gen(int[] cube)
        {
            int is2gen = 0;
            int edges_ori = 0;

            if (cube[1] == cube[4] || cube[16] == cube[13]) edges_ori++;
            if (cube[3] == cube[4] || cube[50] == cube[13]) edges_ori++;
            if (cube[5] == cube[4] || cube[21] == cube[13]) edges_ori++;
            if (cube[7] == cube[4] || cube[37] == cube[13]) edges_ori++;
            if (cube[28] == cube[4] || cube[10] == cube[13]) edges_ori++;
            if (cube[46] == cube[4] || cube[13] == cube[13]) edges_ori++;
            if (cube[19] == cube[4] || cube[14] == cube[13]) edges_ori++;

            if (edges_ori < 7) return 0;

            // corners
            int[] corner_temp = new int[6];
            int[] corners_pos = new int[6];

            // helper: 2**cube[i] -> 1 << cube[i]
            int BIT(int idx) => 1 << cube[idx];

            corner_temp[0] = BIT(6) + BIT(36) + BIT(53); // FDL
            corner_temp[1] = BIT(8) + BIT(38) + BIT(24); // FDR
            corner_temp[2] = BIT(0) + BIT(15) + BIT(47); // FUL
            corner_temp[3] = BIT(2) + BIT(17) + BIT(18); // FUR
            corner_temp[4] = BIT(29) + BIT(9) + BIT(45); // BUL
            corner_temp[5] = BIT(27) + BIT(11) + BIT(20); // BUR

            for (int co = 0; co < 6; co++)
            {
                int v = corner_temp[co];
                if (v == (BIT(4) + BIT(40) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(40) + BIT(22))) corners_pos[co] = 1;
                else if (v == (BIT(4) + BIT(13) + BIT(49))) corners_pos[co] = 2;
                else if (v == (BIT(4) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else if (v == (BIT(31) + BIT(13) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(31) + BIT(13) + BIT(22))) corners_pos[co] = 1;
                else corners_pos[co] = 9; // unknown sentinel
            }

            int coVal = corners_pos[0] * 100000 + corners_pos[1] * 10000 + corners_pos[2] * 1000 + corners_pos[3] * 100 + corners_pos[4] * 10 + corners_pos[5];
            // list from Fortran
            int[] allowed = new int[] { 12201, 1221, 11022, 12120, 10212, 102210, 110220, 100122, 102021, 101202, 210021, 221001, 211200, 210102, 212010, 21102, 2112, 22011, 21210, 20121, 120012, 112002, 122100, 120201, 121020, 201120, 220110, 200211, 201012, 202101 };
            if (allowed.Contains(coVal)) is2gen = 1;
            else return 0;

            // second test: different mapping
            for (int co = 0; co < 6; co++)
            {
                int v = corner_temp[co];
                if (v == (BIT(4) + BIT(40) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(40) + BIT(22))) corners_pos[co] = 1;
                else if (v == (BIT(4) + BIT(13) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else if (v == (BIT(31) + BIT(13) + BIT(49))) corners_pos[co] = 1;
                else if (v == (BIT(31) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else corners_pos[co] = 9;
            }

            coVal = corners_pos[0] * 100000 + corners_pos[1] * 10000 + corners_pos[2] * 1000 + corners_pos[3] * 100 + corners_pos[4] * 10 + corners_pos[5];
            if (allowed.Contains(coVal)) is2gen = 1;
            else is2gen = 0;

            return is2gen;
        }

        // FObj: returns int[2] {10*nmax - x, x}
        public static int[] FObj(int[] cube, int[] cubeSolved, int[] movesSequence, int typeFunc)
        {
            var res = new int[2];
            if (movesSequence == null || movesSequence.Length == 0)
            {
                res[0] = 0;
                res[1] = 0;
                return res;
            }

            int nmax = 0;
            int x = 0;
            var cubeTemp = (int[])cube.Clone();

            for (int i = 0; i < movesSequence.Length; i++)
            {
                cubeTemp = DoMove(cubeTemp, movesSequence[i]);
                int ni = typeFunc switch
                {
                    1 => Compare(cubeTemp, cubeSolved),
                    2 => Entropy(cubeTemp),
                    3 => Is2x2x3(cubeTemp),
                    4 => 10 * Is2gen(cubeTemp) + 10 * Is2x2x3(cubeTemp),
                    _ => 0
                };

                if (ni > nmax)
                {
                    nmax = ni;
                    x = i + 1; // Fortran used 1-based index for x
                }
            }

            res[0] = 10 * nmax - x;
            res[1] = x;
            return res;
        }

        // ClassementPopulation: reorder rows of 2D array descending by val_fobj
        public static int[,] ClassementPopulation(int[,] pop, int[] valFobj)
        {
            int rows = pop.GetLength(0);
            int cols = pop.GetLength(1);
            var indices = Enumerable.Range(0, valFobj.Length)
                                    .Select(idx => new { idx, val = valFobj[idx] })
                                    .OrderByDescending(x => x.val)
                                    .Select(x => x.idx)
                                    .ToArray();

            var outArr = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                int src = indices[i];
                for (int j = 0; j < cols; j++) outArr[i, j] = pop[src, j];
            }

            return outArr;
        }

        // PopMariee: crossover operator (faithful approximation)
        public static int[,] PopMariee(int[,] pop, double phi, double pc, int[,] perfo)
        {
            int n = pop.GetLength(0);
            int m = pop.GetLength(1);
            var result = (int[,])pop.Clone();

            // compute pi array
            double[] pi = new double[n];
            for (int i = 0; i < n; i++)
            {
                pi[i] = 1.0 / n * (phi - i * (2.0 * phi - 2.0) / (n - 1));
            }

            // preserve elite (index 0)
            // fill remaining with children: start at index 1 (Fortran started at 2 because 1-based and kept elite)
            int childIdx = 1;
            while (childIdx < n && childIdx + 1 < n)
            {
                int p1 = 0, p2 = 0;
                // select first parent using rejection described
                while (true)
                {
                    double x0 = RandomDouble();
                    int r0 = (int)(n * 0.9999 * x0);
                    double y0 = RandomDouble();
                    if ((2.0 - phi + 2.0 * y0 * (phi - 1.0)) / n <= pi[r0])
                    {
                        p1 = r0;
                        break;
                    }
                }

                // second parent uniform
                p2 = (int)(n * 0.9999 * RandomDouble());

                // choose crossover point in {1..m-1}
                int cross = (int)((m - 1) * 0.9999 * RandomDouble()) + 1;
                if (cross == m) cross = m - 1;

                if (RandomDouble() < pc)
                {
                    // child A
                    for (int j = 0; j < cross; j++) result[childIdx, j] = pop[p1, j];
                    for (int j = cross; j < m; j++) result[childIdx, j] = pop[p2, j];
                    // child B
                    for (int j = 0; j < cross; j++) result[childIdx + 1, j] = pop[p2, j];
                    for (int j = cross; j < m; j++) result[childIdx + 1, j] = pop[p1, j];
                }
                else
                {
                    // no crossover
                    for (int j = 0; j < m; j++)
                    {
                        result[childIdx, j] = pop[p1, j];
                        result[childIdx + 1, j] = pop[p2, j];
                    }
                }

                childIdx += 2;
            }

            // If odd remaining slot, copy random parent
            if (childIdx < n)
            {
                int p = (int)(n * 0.9999 * RandomDouble());
                for (int j = 0; j < m; j++) result[childIdx, j] = pop[p, j];
            }

            return result;
        }

        // PopMutee: mutation (elite row 0 not mutated)
        public static int[,] PopMutee(int[,] pop, double pm)
        {
            int n = pop.GetLength(0);
            int m = pop.GetLength(1);
            var res = (int[,])pop.Clone();

            for (int i = 1; i < n; i++) // start at 1 to preserve elite
            {
                for (int j = 0; j < m; j++)
                {
                    if (RandomDouble() < pm)
                    {
                        res[i, j] = (int)(18 * 0.9999 * RandomDouble());
                    }
                }
            }

            return res;
        }

        // TrimSequence: reduces redundant or cancelling moves; keeps same array length, fills replaced slots by random moves
        public static int[] TrimSequence(int[] movesSequence)
        {
            if (movesSequence == null) return Array.Empty<int>();
            int ltot = movesSequence.Length;
            var seq = (int[])movesSequence.Clone();

            bool changed;
            do
            {
                changed = false;
                for (int l = 0; l < ltot - 1; l++)
                {
                    int move = seq[l];
                    int next = seq[l + 1];
                    if (move % 6 != next % 6) continue;

                    // Two moves cancel if abs difference == 12
                    if (Math.Abs(move - next) == 12)
                    {
                        // remove l and l+1 by shifting left and fill tail with randoms
                        for (int t = l; t < ltot - 2; t++) seq[t] = seq[t + 2];
                        seq[ltot - 2] = (int)(18 * 0.9999 * RandomDouble());
                        seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                        changed = true;
                        break;
                    }

                    // Now handle face / face2 / face' composition rules (keeps parity)
                    if (move < 6)
                    {
                        if (next == move)
                        {
                            seq[l] = move + 6;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                        else if (next == move + 6)
                        {
                            seq[l] = move + 12;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                    }

                    if (move < 12)
                    {
                        if (move < 6) { /* already handled */ }
                        else
                        {
                            if (next == move - 6)
                            {
                                seq[l] = move + 6;
                                for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                            else if (next == move)
                            {
                                // face^2 then same => cancels
                                for (int t = l; t < ltot - 2; t++) seq[t] = seq[t + 2];
                                seq[ltot - 2] = (int)(18 * 0.9999 * RandomDouble());
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                            else if (next == move + 6)
                            {
                                seq[l] = move - 6;
                                for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                        }
                    }

                    // move >= 12 (face')
                    if (move >= 12)
                    {
                        if (next == move - 6)
                        {
                            seq[l] = move - 12;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                        else if (next == move)
                        {
                            seq[l] = move - 6;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                    }
                }
            } while (changed);

            return seq;
        }

        public static int[] TwistSequence(int[] movesSequence)
        {
            if (movesSequence == null) return Array.Empty<int>();
            var res = new int[movesSequence.Length];
            for (int i = 0; i < movesSequence.Length; i++)
            {
                int m = movesSequence[i];
                // formula from Fortran: 6*(m/6) + mod(1+m-6*(m/6),3) + 3*((m-6*(m/6))/3)
                int baseFace = (m / 6) * 6;
                int part1 = (1 + m - baseFace) % 3;
                int part2 = 3 * ((m - baseFace) / 3);
                res[i] = baseFace + (part1 + part2);
            }
            return res;
        }

        public static int[] TwistCube(int[] c)
        {
            var t = new int[54];
            // F <- R
            t[0] = c[24]; t[1] = c[21]; t[2] = c[18];
            t[3] = c[25]; t[4] = c[22]; t[5] = c[19];
            t[6] = c[26]; t[7] = c[23]; t[8] = c[20];
            // U <- F
            t[9] = c[6]; t[10] = c[3]; t[11] = c[0];
            t[12] = c[7]; t[13] = c[4]; t[14] = c[1];
            t[15] = c[8]; t[16] = c[5]; t[17] = c[2];
            // R <- U
            t[18] = c[17]; t[19] = c[16]; t[20] = c[15];
            t[21] = c[14]; t[22] = c[13]; t[23] = c[12];
            t[24] = c[11]; t[25] = c[10]; t[26] = c[9];
            // B <- L
            t[27] = c[47]; t[28] = c[50]; t[29] = c[53];
            t[30] = c[46]; t[31] = c[49]; t[32] = c[52];
            t[33] = c[45]; t[34] = c[48]; t[35] = c[51];
            // D <- B
            t[36] = c[33]; t[37] = c[30]; t[38] = c[27];
            t[39] = c[34]; t[40] = c[31]; t[41] = c[28];
            t[42] = c[35]; t[43] = c[32]; t[44] = c[29];
            // L <- D
            for (int i = 45; i <= 53; i++) t[i] = c[36 + (i - 45)];
            return t;
        }

        public static int[] RotateWholeCube(int[] cu)
        {
            var t = new int[54];
            // U
            t[9] = cu[15]; t[10] = cu[12]; t[11] = cu[9];
            t[12] = cu[16]; t[13] = cu[13]; t[14] = cu[10];
            t[15] = cu[17]; t[16] = cu[14]; t[17] = cu[11];
            // D
            t[36] = cu[38]; t[37] = cu[41]; t[38] = cu[44];
            t[39] = cu[37]; t[40] = cu[40]; t[41] = cu[43];
            t[42] = cu[36]; t[43] = cu[39]; t[44] = cu[42];
            // F <- R
            for (int i = 0; i < 9; i++) t[i] = cu[18 + i];
            // R <- B
            for (int i = 0; i < 9; i++) t[18 + i] = cu[27 + i];
            // B <- L
            for (int i = 0; i < 9; i++) t[27 + i] = cu[45 + i];
            // L <- F
            for (int i = 0; i < 9; i++) t[45 + i] = cu[i];
            return t;
        }

        public static int[] RotateWholeCubeX(int[] cu)
        {
            // rotate around LR axis: twist_cube then 3 * rotate_whole_cube
            var res = TwistCube(cu);
            for (int rot = 1; rot <= 3; rot++) res = RotateWholeCube(res);
            return res;
        }

        // Solve twogen by exhaustive search (brute-force). This implements an
        // iterative-deepening DFS limited to moves in the 2-gen group (F and U
        // generators with their 3 variants: normal, double, inverse).
        public static void SolveTwogenBourrin(int[] cube2gen, int[] cubeSolved)
        {
            if (cube2gen == null || cubeSolved == null) return;

            // Allowed moves in 2-gen (F and U, with variants): 0,1,6,7,12,13
            int[] allowedMoves = new int[] { 0, 1, 6, 7, 12, 13 };

            // Maximum search depth (configurable). Beware: complexity ~ 6^depth.
            const int maxDepth = 20;

            int[] currentSeq = new int[maxDepth];
            bool found = false;
            int foundDepth = -1;

            // Helper recursive DFS (depth-limited). We avoid consecutive moves on same face (move%6)
            // because variants exist (e.g. F then F2 can be represented by a single symbol).
            bool Dfs(int[] cubeState, int depthLeft, int lastFace, int pos)
            {
                // Check solved at current prefix (this allows shorter solutions)
                if (Compare(cubeState, cubeSolved) == 54)
                {
                    found = true;
                    foundDepth = pos;
                    return true;
                }

                if (depthLeft == 0) return false;

                foreach (var move in allowedMoves)
                {
                    int face = move % 6;
                    if (lastFace != -1 && face == lastFace) continue; // prune consecutive same face

                    currentSeq[pos] = move;
                    var nextCube = DoMove(cubeState, move);

                    if (Dfs(nextCube, depthLeft - 1, face, pos + 1)) return true;
                }

                return false;
            }

            // Iterative deepening
            for (int depth = 0; depth <= maxDepth && !found; depth++)
            {
                Console.WriteLine("Brute force depth : {0}", depth);
                var startCube = (int[])cube2gen.Clone();
                if (Dfs(startCube, depth, -1, 0))
                {
                    found = true;
                    break;
                }
            }

            if (found && foundDepth > 0)
            {
                var solution = currentSeq.Take(foundDepth).ToArray();
                var notation = ToNotation(solution);
                Console.WriteLine("youpie ! (bourrin) solution found");
                Console.WriteLine(string.Join("", notation));
                // Append to results
                try
                {
                    System.IO.File.AppendAllText("results.txt", "SolveTwogenBourrin (sol) : " + string.Join("", notation) + Environment.NewLine);
                }
                catch
                {
                    // best effort, ignore IO errors here
                }
            }
            else if (found && foundDepth == 0)
            {
                Console.WriteLine("Cube already solved (bourrin).");
                try
                {
                    System.IO.File.AppendAllText("results.txt", "SolveTwogenBourrin : cube already solved" + Environment.NewLine);
                }
                catch { }
            }
            else
            {
                Console.WriteLine($"SolveTwogenBourrin: no solution found up to depth {maxDepth}.");
                try
                {
                    System.IO.File.AppendAllText("results.txt", $"SolveTwogenBourrin : no solution up to depth {maxDepth}" + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}