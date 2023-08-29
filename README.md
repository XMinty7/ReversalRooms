# About
This repository hosts a community made FNA port of the video game [Reversal Rooms](https://taelsdafoox.itch.io/reversal-rooms) made by [TaelsDaFoox](https://taelsdafoox.itch.io) for the [GMTK Game Jam 2023](https://itch.io/jam/gmtk-2023). The goal is to create a more open and mod-able version of the game.

# Building
The project references FNA but it is not included.

- Clone the repository.
- Clone the [FNA](https://github.com/FNA-XNA/FNA) repository into the root directory (so that it ends up in ./FNA).
```$ git clone --recursive https://github.com/FNA-XNA/FNA```
- Use your .NET IDE or CLI of choice (with support for .NET Framework) to build the project.
- Put the FNA native binaries that correspond to your target platform in the root of the build output folder. You can compile them yourself from the FNA project or download precompiled binaries from this link: https://fna.flibitijibibo.com/archive/fnalibs.tar.bz2
- You should have a running build! If you encounter any issues, review the [FNA wiki](https://github.com/FNA-XNA/FNA/wiki) and particularly the [FAQ page](https://github.com/FNA-XNA/FNA/wiki/0:-FAQ).

# Contributing
Contributions are welcome and appreciated. Fork the repository, make your changes and submit a pull request, then someone will review it and merge it if appropriate.

# Discord
Join the [Discord server](https://discord.gg/t7UUvxwb3Q) to notify us of your contributions, ask to be a contributor on GitHub, talk about the game or just chat.