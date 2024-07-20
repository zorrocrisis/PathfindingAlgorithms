## **Pahtfinding Algorithms**
This project, originally an evaluation component for the Artificial Intelligence in Games course (2023/2024), talking place in Instituto Superior Técnico, University of Lisbon, aimed to showcase multiple **pathfinding algorithms in video games**, additionally seeking to **research and further strengthen their efficiency**. 

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

## **Efficiency Analysis - Introduction **
In order to study the efficiency of pathfinding algorithms in video games, the following **main algorithms** were implemented in C# (and Unity v.2021.3.10f1): **Dijkstra** (A Star with Zero Heuristic), **A Star** (with Euclidean Distance Heuristic), **Node Array A Star** and **Goal Bound A Star**.

Additionally, the following **secondary optimisations** were implemented, **influencing the manner through which the grid nodes are stored and managed** in the Open and Closed sets: **A Star with Closed Dictionary**, **A Star with Closed Dictionary and Open Priority Heap**.


WIPPPPPPPPPPPP
Within Unity, these algorithms can be turned on by enabling the different *Pathfinding Settings* in the Manager’s inspector window. Additionally, the utilized grid map can be changed simply by editing the *Grid Name* property under *Grid Settings*, also in the Manager’s inspector window.

Below follows an analysis of the performance of the mentioned pathfinding algorithms, done with the aid
of Unity’s Profilertool. The path tested was the pre-defined path associated with key 3, using 100 nodes
per search and was performed in the giant grid. Notably, the most recent node was used to solve ties in
the algorithms.

Note: The methods RemoveFromOpenand Replacewere omitted from the analysis since they weren’t used in the algorithm.

## **Efficiency Analyis - Results**

- A* Pathfinding – Performance Improvements
With the goal of improving the overall performance of the A* algorithm (with the Euclidean Distance heuristic implemented), different data structures were used for the Open and Closed lists. This eliminates the major performance issues of accessing, adding, and removing nodes, as we can infer from the overall reduction of the average execution times of the different methods used.

(IMAGE HERE)

After using the dictionary data structure for the closed set, the performance of the two last methods listed (related to operations using the closed set) drastically improved, having a great impact in the execution time of the A*Pathfinding.Searchmethod. More specifically, the execution time of SearchInClosedand RemoveFromCloseddecreased 120 times and 30 times, respectively.

Considering the large size of the closed set – up to 2606 nodes using the path chosen – it makes sense to implement a data structure that can easily retrieve any needed nodes, thus increasing the overall performance of the algorithm by decreasing the time spent searching for information. The dictionary allows exactly for a faster means of retrieving nodes since it utilizes a hash lookup, whereas in an unordered list we needed to rely on iteration (i.e. going through the whole list until we find the result).

(IMAGE HERE)

With the intent of further improving the performance of the A* algorithm, a priority heap data structure was used for the open set, in addition to the previous dictionary for the closed set. This implementation slightly reduced the execution time of SearchInOpen(about 2 times lower than the previous value), and although the AddToOpen took, on average, longer to execute, the execution time of GetBestAndRemove also decreased (again, about 2 times lower). The execution time of the main method, A*Pathfinding.Search, also decreased 1.3 times.

- Node Array A* Pathfinding
The implementation of the Node Array A* algorithm (with Euclidean Distance) allowed trading memory for speed by eliminating the need of having a closed set but creating an array with all existing nodes in order to keep track of their status (Unvisited, Open or Closed).

(IMAGE HERE)

Comparing with the data in Table 1, the node record array A*’s SearchInClosedmethod became 688 times faster and 6 times faster when comparing with Table 2 and 3. Also, the RemoveFromClosedgot more than 151 times faster in relation to the that of the Table 1. We can see that the AddToClosed, SearchInClosedand RemoveFromClosedmethods benefit the most by using this algorithm.

The main method’s (A*Pathfinding.Search) overall execution time was about 1.3 times faster than the one registered in Table 3, meaning that the memory-time trade-off can be somewhat rewarding if memory isn’t a significant issue. It’s also worth noticing that while this version of the algorithm processes 19820 nodes, the algorithm with a priority heap for the open set and a dictionary for the closed set process a total of 19925 nodes

- Goal Bound A* Pathfinding
Although the Goal Bounding A* algorithm was not fully implemented, some tests were performed with a primitive, “hard-coded” version and the results showed a significant decrease in the amount of total processed nodes by giving the algorithm a preferred search direction, which lowered the execution times greatly. Similarly, to the node array A*, there is also a trade-off: preprocessing the map can take long periods of time... If the grid used doesn’t change with time, however, we can see how a full implementation could be viable.

(IMAGE HERE)

- Overview
All in all, we can state the Node Record Array A* algorithm displayed the best performance by keeping an array as a record of all the nodes and their corresponding status, thus eliminating the need for a closed set structure altogether.

Even more, looking at the results from Table 1, 2 and 3, we can safely state that this algorithm could be easily further improved by implementing a more efficient data structure for the open set, such as the priority heap. Instead of an array, a different way of storing nodes and their attributes is a worthwhile possibility to optimize the algorithm.

By implementing this algorithm, however, one must consider the memory requirements of the node array - a bigger map/grid will result in a more significant storage necessity for this algorithm, which could make other options more attractive

(IMAGE HERE)

## **Program's Dependencies and Assets**
Unity Version

## **Authors and Acknowledgment**

This shader project was developed by **[Miguel Belbute (zorrocrisis)](https://github.com/zorrocrisis)**

The initial code was contributed by **[Prof. Carlos Martinho](https://fenix.tecnico.ulisboa.pt/homepage/ist14181)**

