# RubikGeneticSolver

**RubikGeneticSolver** is a fully automatic **3×3×3 Rubik’s Cube solver** based on a genetic algorithm approach.

Unlike most modern solvers, this project **does not rely on any pre-defined move sequences** (often called *“algorithms”* in the cubing community) nor on **pre-computed optimal lookup tables**. Instead, it follows a **three-step, human-like solving method**.

While it is **not an optimal solver**, it typically produces solutions in **25 to 40 moves**, which is quite respectable for a knowledge-free approach.

---

## Solving Method

The solver tackles the cube sequentially using the following steps:

1. Solve a 2×2×3 block at the back of the cube  
2. Orient the remaining edges and resolve corner parity  
3. Solve the remaining pieces in the *(U, F)* 2-generator subgroup  

---

## Project History

I originally started this project in **2005**.  
Nearly **20 years later**, I revisited the source code, and—with the help of a very nice AI assistant—translated it from **Fortran 90** to **C#**.

If you’re interested in the background and evolution of the project, feel free to read more on my website:

https://www.francocube.com/cyril/genetic_alg
