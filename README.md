
# Evolution Sim

A Unity-based simulation of evolving creatures in a dynamic environment. Creatures use neural networks to perceive, move, and survive, with evolution driven by natural selection and mutation over multiple epochs.

## Features

- **Procedural Terrain:** Uses Perlin noise and wave-based mesh generators for land and water.
- **Creatures:** Agents with neural networks that process vision and internal state to control movement.
- **Evolution:** Creatures evolve over epochs, with the best performers passing on and mutating their neural weights.
- **Plant Life:** Plants spawn and age, providing food for creatures.
- **Temperature System:** Environmental temperature affects plant growth and water simulation.
- **Inspector Controls:** All major parameters are exposed for easy tuning in the Unity Editor.

## Key Scripts

- `EvolutionSimController.cs` — Manages epochs, spawning, and evolution.
- `BrainScript.cs` — Handles neural network logic for each creature.
- `AnimalVisionScript.cs` — Simulates vision using raycasts.
- `CreatureMovementScript.cs` — Controls movement and boundary logic.
- `PerlinMeshGenerator.cs` / `WaterMeshGenerator.cs` — Generate and tile terrain meshes.
- `PlantLifeScript.cs` — Handles plant growth, aging, and nutrition.
- `TemperatureControllerScript.cs` — Controls environmental temperature.

## Saving & Loading

- Neural network weights are saved and loaded as CSV files for persistent evolution.
