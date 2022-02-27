Start learning hex map tutorial on catlikecoding.com.

## T1 ##
Create grid cells and meshes, change color when touch one cell.

But the cells (and its meshes) of the whole map would be recreated when changing color, this should be fixed in the following tutorial.

## T2 ##
Use bridge connect neighbor cells, and set blend colors.

But the edge cells would be smaller than others because bridge only exist between neighbor cells.

## T3 ##
Add elevation to cells, and deal with different connection edge type. Learning how to implement terraces.

Now I know why recreate cells each time when change cell's property(color,elevation): the whole map is created
as a whole mesh from cells. If each cell has same count vertices / colors, it is possible to get changed cell's
vertices and colors indexes, but because of different type of edge connection type, each cell's vertices/colors 
count is different too, its hard to index the vertices and colors when a cell's property has been changed.

## T4 ##
Randomization makes things be more complex.

Need to deal with color for different elevation.

## T5 ##
Chunks, camera, brush

Chunks solved the problem presented in T1 for large map.

## T6 ##
Rivers can be implemented in 3 ways:
1. let then flow from cell to cell.(Endless Legend, this tutorial)
2. let them flow in between cells, from edge to edge. (CIV5)
3. use water cell to suggest rivers. (Age of Wonders 3)

Rivers make things be more complex.
XXX： Only one flow river each cell, multiple rivers not supported.

## T7 ##
Roads' implementation is similar to rivers, but roads must support crossroads.

Roads & rivers implementation is most about generating mesh.

## T8 ##
Water cell is supported now.

I need to learn more about how to write a shader file.