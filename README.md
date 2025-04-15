# COMP4029 Dissertation

This repository contains all the code and configuration files needed for my MSci COMP4029 dissertation project. Developing and Optimizing AI for Hex. It is an implementation of Hex game on Unity with different AI players. 
You can play locally against peers, play against different AI or even simulate a game between AI.

## Folders
📁 Hex_Game

This single folder contains all the Unity related files including the Assets, Packages and Project Settings. The inside of the Assets folder contains all the source code, scenes, images, sprites and the likes.

🏃 To run the game, you will need to:

- Clone this repository into your local folders, if preferred you can use a different method such as downloading the zipped version.
- Download and open Unity Hub, the specific editor version used to develop this is 2021.3.13f1, which you might need to download from the Unity website.
- Simply import the local copy of this project into your Unity Hub, select the version mentioned above, you can choose to skip this, however, it may introduce errors or bugs.
- If an error message pops up saying the project is not valid, it is because you opened the master folder, you should select the Hex_Game folder instead.
- After importing it, you can open the project, there are some missing files on this repository but they will all be created once Unity builds the project.

🚗 Navigating the game is very simple:
- the main menu is the first scene you should open, you can do this by going into assets/scenes and selecting main menu.
- Within the main menu, you have 3 buttons, one to play the game locally, one to play against AI, and one to simulate a game between AI.
- Any button you select, will move you to a new scene, which is the game board scene. The logic is different depending on the button you clicked.
- If playing against AI, you will play the first move and the AI will be the second player.
- If simulating a game, it will be automatic play between 2 AI, which resets after a winner is decided, this is repeated until one AI reaches 15 wins.
- 
🤖 To change the AI you are playing against:
- You can navigate to scripts inside the assets folder, and select the GameTiles.cs file, there in line 141, you can change the caller of the function to mtcs, mtcsBasic, mtcsRave, alphaBeta or heuristicAgent.
- If you want to simulate different AI, you can change this in the SimulateAIGame method, in the same way as the previous point.

The GameTiles.cs file is the main file that contains all the game logic, this includes enforcing the game rules, coloring a hex, calling AI moves, checking a player for wins etc. 

There are various files containing the AI agents, the best performing one being MTCS.cs, which is the heuristic-based MTCS agent. 

