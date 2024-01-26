# Hashing Simulation
Visualizing pseudorandomness using hash values in a 3D plane.
 
<img src="/Assets/PNG/hashing-example.PNG" alt="Hashing Visualization" style="width: 70%"> 

Hash method used is a partial variant of the xxHash algorithm by Yann Collet and customized by Jasper Flick. It consumes the UV coordinates of a flat plane, employs bit shifting and rotations, and produces an avalanche effect manipulating the pattern, coloring, and height displacement.

<img src="/Assets/PNG/DomainTRS.gif" alt="Hashing Visualization" style="width: 90%"> 

Domain TRS is added to manipulate the hashed plane's shape to study more about transformations.

### Note:
This is a short case study replicated for visualizing different pseudorandom patterns using Unity 3D.
