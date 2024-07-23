## **Pahtfinding Algorithms**
This project, originally an evaluation component for the Artificial Intelligence in Games course (2023/2024), talking place in Instituto Superior Técnico, University of Lisbon, aimed to showcase multiple **pathfinding algorithms**, additionally seeking to **research and further strengthen their efficiency**. 

![pathfinding](https://github.com/user-attachments/assets/5d6efe70-b4eb-4c56-9da0-1b2ae98aad88)

The following document indicates how to access the source code, utilise the executable application and control the program. It also contains an efficiency analysis between the pathfinding algorithms. 

## **Source Files and Application**
The project's source files can be downloaded from this repository. To open the program using Unity (v.2021.3.10f1), simply clone the repository and open the project utilising Unity Hub.

To test the application, only the files contained in the "Build" folder are necessary. The application can also be found on [this separate repoitory](https://github.com/zorrocrisis/PathfindingAlgorithmsBuildOnly/tree/main). Once the build files are downloaded, simply start the executable (see the controls below).

## **Application's Controls**

Main Menu:
- **LMB** interacts with the main menu's buttons, selecting the grid size and exiting the application.

Grid Map:
- **Esc** exits to the main menu.
- **1-3** begins the pathfinding with pre-defined starting and goal positions.
- **LMB** selects the starting and goal positions for a new pathfinding search.
- **Space** clears the grid.
- **Left/Right arrow keys** change the current algorithm.

## **Additional Pathfinding Information**
One can find additional pathfinding information, such as positional coordinates, total processed nodes and total process time, in the **overlay UI panel in right-side of the screen**. During a pathfinding search, the grid will also showcase multiple **coloured tiles**, representing different states: **red tiles indicate "closed" nodes** (which have already been explored), whereas **blue tiles indicate "open" ondes (which have not been explored yet)**. The goal bound A* algorithm also paints the tiles according to the corresponding boundary boxes, though these colours overlap (yellow - "up" is the best starting edge; green - "down" is the best starting edge; light blue - "left" is the best starting edge; purple - "right" is the best starting edge).

![imagem](https://github.com/user-attachments/assets/38eb7030-c791-4cf8-9ede-37957abfc50a)

## **Efficiency Analysis - Introduction**
In order to study the efficiency of pathfinding algorithms in video games, the following **main algorithms** were implemented in C# (and Unity v.2021.3.10f1): **Dijkstra** (A Star with Zero Heuristic), **A Star** (with Euclidean Distance Heuristic), **Node Array A Star** (with Euclidean Distance Heuristic) and **Goal Bound Node Array A Star** (with Euclidean Distance Heuristic).

Additionally, the following **secondary optimisations** were implemented, **influencing the manner through which the grid nodes are stored and managed** in the Open and Closed sets: **A Star with Closed Dictionary** and **Open Priority Heap**.

Within Unity, all algorithms can be turned on by enabling the corresponding *Pathfinding Settings* in the Manager’s inspector window. Additionally, the utilized grid map can be changed simply by editing the *Grid Name* property under *Grid Settings*, also in the Manager’s inspector window.

Below follows a performance analysis of the aforementioned pathfinding algorithms, performed with the aid of Unity’s Profilertool. The tested path was the pre-defined path associated with the number key "3" (see image below), using 100 nodes per search and the giant grid. The most recent node was used to solve ties in the algorithms.

![imagem](https://github.com/user-attachments/assets/f6bbf793-d34d-4cf4-97de-435a0062ae7f)


## **Efficiency Analyis - Results**

- **A Star Pathfinding – Improving the Open and Close Sets**
With the goal of improving the overall performance of the A* algorithm (with the Euclidean Distance heuristic), different data structures were used for the Open and Closed lists. This would, in theory, eliminate the major performance issues of accessing, adding, and removing nodes.

![imagem](https://github.com/user-attachments/assets/0303d3c5-096d-413d-a879-c7b67913a45e)

Considering the large size of the Closed set – up to 2606 nodes using the chosen path – it makes sense to implement **data structures which can easily retrieve any needed nodes**, thus increasing the overall performance of the algorithm by decreasing the time spent searching for information. A dictionary data structure allows exactly for a faster means of retrieving nodes since it utilises a hash lookup, whereas an unordered list (employed in A Star with Zero and Euclidean Heuristics) relies on iteration (i.e. going through the whole list until we find the desired node). 

After using the dictionary data structure for the Closed set (A Star with Closed Dictionary), we can indeed verify the performance of the two last methods listed (related to operations using the Closed set) drastically improved, having a great impact in the execution time of the *AStarPathfinding.Search* method. More specifically, the execution time of *SearchInClosed* and *RemoveFromClosed* decreased 120 times and 30 times, respectively.

![imagem](https://github.com/user-attachments/assets/99a4ddd7-89b1-495e-90d1-1711f30e858c)

With the intent of further improving the performance of the A Star algorithm, a priority heap data structure was used for the Open set, in addition to the previous dictionary for the Closed set (A Star with Closed Dictionary and Open Priority Heap). This implementation slightly reduced the execution time of SearchInOpen (about 2 times lower than the previous value), and although the *AddToOpen* took, on average, longer to execute, the execution time of *GetBestAndRemove* also decreased (again, about 2 times lower). The execution time of the main method, *AStarPathfinding.Search*, also decreased 1.3 times.

![imagem](https://github.com/user-attachments/assets/cfcb96a6-4d3c-451d-a18c-b7052901fd0d)

- **Node Array A Star**
The implementation of the Node Array A Star algorithm (with Euclidean Distance heuristic) allowed **trading memory for speed** by eliminating the need of having a Closed set but creating an array with all existing nodes in order to keep track of their status (Unvisited, Open or Closed).

![imagem](https://github.com/user-attachments/assets/16f665fc-c0a8-41f2-bbc3-70b4da865244)

Comparing with the data in Table 1, the node record array A Star’s *SearchInClosedmethod* became 688 times faster and 6 times faster when comparing with Table 2 and 3, respectively. Also, the *RemoveFromClosed* got more than 151 times faster in relation to the original A Star algorithm in Table 1. We can also verify that the *AddToClosed*, *SearchInClosed* and *RemoveFromClosedmethods* benefit the most by using this algorithm.

The main method’s (*AStarPathfinding.Search*) overall execution time was about 1.3 times faster than the one registered in Table 3, meaning that **the memory-time trade-off can be rewarding if memory does not represent a significant issue**. It is also worth noticing that while this version of the algorithm processed 19820 nodes, the algorithm with a priority heap for the Open set and a dictionary for the Closed set processed a total of 19925 nodes (slightly higher).

- **Goal Bound Node Array A Star**
The Goal Bound Node Array A Star implementation, displayed very similar results to the Node Array A Star algorithm (without the Goal Bounds), as seen in Table 5 and considering the processed nodes and processing time - NodeArray: PNodes = 19925, PTime = 6.23s; GoalBoundNodeArray: PNodes = 19925, PTime = 6.47s. This is unsurprising as the first algorithm is grounded on the latter, only **offering preferred search directions for the pathfinding**. However, if the preferred search direction is "obvious" (as in the case of the default starting position 3) or simply the only one available, the goal bound component of the algorithm will not provide an improved efficiency, behaving like its base algorithm.

![imagem](https://github.com/user-attachments/assets/ceb969e2-91c5-4b46-826e-c2481f10147f)

On the other hand, if we consider the giant grid's default position 2 (accessed by pressing "2" on the keyboard), whose starting locations displays less "obvious" paths to the goal node, the Goal Bound Node Array A Star algorithm (second image below) demonstrates a **significant decrease in the amount of total processed nodes and total processing time by exploiting preferred search directions**, when compared to the Node Array A Star algorithm (first image below) - NodeArray: PNodes = 7725, PTime = 2.45s; GoalBoundNodeArray: PNodes = 3854, PTime = 1.49s. In this specific scenario, the combined efforts of the goal bound and node array mechanisms provide less than 0.5 times the processed nodes and approximately 0.6 times the processed time. These results are emphasized in larger maps, of course - in the medium grid, for instance, it would be more difficult to distinguish the performance improvement of the goal bound system.

![NodeArrayPos3](https://github.com/user-attachments/assets/edb1715b-7d4c-4a4e-abec-16a0504df4ec)
![GoalBoundPos2](https://github.com/user-attachments/assets/58f66f1e-242f-4c77-9515-7feb42774fbd)

Similarly, to the Node Array A Star, there is also a **trade-off: preprocessing the map can take a considerable period of time**, despite representing a task which can be easily achieved through parallel computing... In summary, if the utilised grid map has a considerable size and is static (meaning it does not change over time), preprocessing the map and exploiting the goal bound mechanisms can offer a worthwile option in terms of pathfinding efficiency. In the specific case of the Goal Bound Node Array A Star algorithm, in the worst case scenario, it behaves as a Node Array A Star, and in the best possible circumstances, it greatly improves efficiency.

- **Analysis Overview**
All in all, we can state **the Goal Bound Node Record Array A Star algorithm displayed the best overall performance** by keeping an array as a record of all the nodes and their corresponding status, thus eliminating the need for a Closed set structure altogether, and by exploiting preferred search directions.

Even more, **this algorithm can be easily improved by implementing a more effective heuristic** and/or applying a different way of storing nodes and their attributes, instead of an array.

By implementing the Goal Bound Node Record Array A Star algorithm, however, **one must consider the memory requirements of the node array - a bigger map/grid will result in a more significant storage necessity for this algorithm**, which could make other options more attractive. Even more, despite the possibility of selectively disregarding portions of the map altogether, **preprocessing a (sometimes a very big) map can be a computationally heavy task**, though one which can be mitigated by harnessing the power of parallel computing.


## **Authors and Acknowledgements**

This project was developed by **[Miguel Belbute (zorrocrisis)](https://github.com/zorrocrisis)** with contributions from Guilherme Serpa and Peng Li.

The initial code was supplied by **[Prof. Pedro dos Santos](https://fenix.tecnico.ulisboa.pt/homepage/ist12886)**

