## **Pahtfinding Algorithms**
This project, originally an evaluation component for the Artificial Intelligence in Games course (2023/2024), talking place in Instituto Superior Técnico, University of Lisbon, aimed to showcase multiple **pathfinding algorithms in video games**, additionally seeking to **research and further strengthen their efficiency**. 

(GIF SHOWING PATHFINDING)

The following document indicates how to access the source code, utilise the executable application and control the program. It also contains an efficiency analysis between the pathfinding algorithms. 

## **Source Files and Application**
The project's source files can be downloaded from this repository. To open the program using Unity (v.2021.3.10f1), simply clone the repository and open the project utilising Unity Hub.

To test the application, only the files contained in the "Build" folder are necessary. Once those files are downloaded, simply start the executable (see the controls below).

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
One can find additional pathfinding information, such as position coordinates and total process time, in the **overlay UI panel in right-side of the screen**. During a pathfinding search, the grid will also showcase multiple **coloured tiles**, representing different states: **red tiles indicate "closed" nodes** (which have already been explored), whereas **blue tiles indicate "open" ondes (which have not been explored yet)**. The goal bound A* algorithm also paints the tiles according to the corresponding boundary boxes, though these colours overlap (yellow - "up" is the best starting edge; green - "down" is the best starting edge; light blue - "left" is the best starting edge; purple - "right" is the best starting edge).

(IMAGE SHOWING UI PANEL AND COLOURS)

## **Efficiency Analysis - Introduction**
In order to study the efficiency of pathfinding algorithms in video games, the following **main algorithms** were implemented in C# (and Unity v.2021.3.10f1): **Dijkstra** (A Star with Zero Heuristic), **A Star** (with Euclidean Distance Heuristic), **Node Array A Star** and **Goal Bound A Star**.

Additionally, the following **secondary optimisations** were implemented, **influencing the manner through which the grid nodes are stored and managed** in the Open and Closed sets: **A Star with Closed Dictionary** and **Open Priority Heap**.

Within Unity, all algorithms can be turned on by enabling the corresponding *Pathfinding Settings* in the Manager’s inspector window. Additionally, the utilized grid map can be changed simply by editing the *Grid Name* property under *Grid Settings*, also in the Manager’s inspector window.

Below follows a performance analysis of the aforementioned pathfinding algorithms, performed with the aid of Unity’s Profilertool. The tested path was the pre-defined path associated with key 3, using 100 nodes per search and the giant grid. The most recent node was used to solve ties in the algorithms.


## **Efficiency Analyis - Results**

- **A Star Pathfinding – Improving the Open and Close Sets**
With the goal of improving the overall performance of the A* algorithm (with the Euclidean Distance heuristic), different data structures were used for the Open and Closed lists. This would, in theory, eliminate the major performance issues of accessing, adding, and removing nodes.

![imagem](https://github.com/user-attachments/assets/62b63a12-cb08-4bdb-97ac-0a1af28f5c6a)

Considering the large size of the Closed set – up to 2606 nodes using the chosen path – it makes sense to implement **data structures which can easily retrieve any needed nodes**, thus increasing the overall performance of the algorithm by decreasing the time spent searching for information. A dictionary data structure allows exactly for a faster means of retrieving nodes since it utilises a hash lookup, whereas an unordered list (employed in A Star with Zero and Euclidean Heuristics) relies on iteration (i.e. going through the whole list until we find the desired node). 

After using the dictionary data structure for the Closed set (A Star with Closed Dictionary), we can indeed verify the performance of the two last methods listed (related to operations using the Closed set) drastically improved, having a great impact in the execution time of the *AStarPathfinding.Search* method. More specifically, the execution time of *SearchInClosed* and *RemoveFromClosed* decreased 120 times and 30 times, respectively.

![imagem](https://github.com/user-attachments/assets/aa5531c3-99e2-4e15-9559-f6e963257567)

With the intent of further improving the performance of the A Star algorithm, a priority heap data structure was used for the Open set, in addition to the previous dictionary for the Closed set (A Star with Closed Dictionary and Open Priority Heap). This implementation slightly reduced the execution time of SearchInOpen (about 2 times lower than the previous value), and although the *AddToOpen* took, on average, longer to execute, the execution time of *GetBestAndRemove* also decreased (again, about 2 times lower). The execution time of the main method, *AStarPathfinding.Search*, also decreased 1.3 times.

![imagem](https://github.com/user-attachments/assets/76de2afe-6557-4f9f-8fe3-a316a47317c5)


- **Node Array A Star**
The implementation of the Node Array A Star algorithm (with Euclidean Distance heuristic) allowed **trading memory for speed** by eliminating the need of having a Closed set but creating an array with all existing nodes in order to keep track of their status (Unvisited, Open or Closed).

![imagem](https://github.com/user-attachments/assets/e10d223a-1d2a-4726-8132-de1af982f849)

Comparing with the data in Table 1, the node record array A Star’s *SearchInClosedmethod* became 688 times faster and 6 times faster when comparing with Table 2 and 3, respectively. Also, the *RemoveFromClosed* got more than 151 times faster in relation to the original A Star algorithm in Table 1. We can also verify that the *AddToClosed*, *SearchInClosed* and *RemoveFromClosedmethods* benefit the most by using this algorithm.

The main method’s (*AStarPathfinding.Search*) overall execution time was about 1.3 times faster than the one registered in Table 3, meaning that **the memory-time trade-off can be rewarding if memory does not represent a significant issue**. It is also worth noticing that while this version of the algorithm processed 19820 nodes, the algorithm with a priority heap for the Open set and a dictionary for the Closed set processed a total of 19925 nodes (slightly higher).

- **Goal Bound A Star**
The Goal Bound A Star algorithm displayed a **significant decrease in the amount of total processed nodes by giving the algorithm preferred search directions**, which lowered the execution times greatly, especially considering the dimensions of the tested grid map. Similarly, to the node array A*, there is also a **trade-off: preprocessing the map can take a considerable period of time**, despite representing a task which can be easily achieved through parallel computing... In summary, if the utilised grid map has a considerable size and is static - meaning it does not change over time - preprocessing the map and exploiting the goal bound mechanisms can offer a worthwile option in terms of pathfinding efficiency.

(GOAL BOUND IMAGE HERE)

- **Analysis Overview**
All in all, we can state **the Node Record Array A Star algorithm displayed the best overall performance** by keeping an array as a record of all the nodes and their corresponding status, thus eliminating the need for a Closed set structure altogether.

Even more, looking at the results from Table 1, 2 and 3, we can safely state **this algorithm can be easily further improved by implementing a more efficient data structure for the Open set**, such as a priority heap. Instead of an array, a different way of storing nodes and their attributes is a promissing possibility to optimize the algorithm.

By implementing the Node Record Array A Star algorithm, however, **one must consider the memory requirements of the node array - a bigger map/grid will result in a more significant storage necessity for this algorithm**, which could make other options, such as the Goal Bound A Star, more attractive.

**Despite the computationally heavy task of preprocessing the (sometimes a very big) map**, which can be mitigated by harnessing the power of parallel computing, **selectively disregarding portions of the map altogether highlight the Goal Bound A Star algorithm**.


## **Authors and Acknowledgements**

This project was developed by **[Miguel Belbute (zorrocrisis)](https://github.com/zorrocrisis)** with contributions from Guilherme Serpa and Peng Li.

The initial code was supplied by **[Prof. Pedro dos Santos](https://fenix.tecnico.ulisboa.pt/homepage/ist12886)**

