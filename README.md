som-demos
=========

A collection of Kohonen Self-Organizing Map demo applications.

_Note:_ These demos were originally created in December 2005. While the source
is not the cleanest, it still hopefully serves as a good learning reference.

## SOM_Color

SOM_Color demonstrates the basic Self-Organizing Map algorithm. It will
attempt to take the given input colors and represent them on a 2-D network of nodes.

![SOM_Color](https://raw.githubusercontent.com/doggan/som-demos/screenshots/screenshots/som_color.jpg "SOM Color")

### Overview and Options

Windows:
 * Initialization window (top left)
  * This window shows the weight distribution of the network at initialization.
 * Training window (top right)
  * This window will be updated each frame, showing the current state of the SOM network. Initially, it will be a copy of the initialization window.
 * BMU window (bottom left)
  * This window will be calculated upon successful training. It will display the most commonly used BMU nodes.
 * Error Map window (bottom right)
  * This window will show the error map of the network. Whites represent higher error, while blacks are lower error.

Initializations:
 * Random: Fill the network with random values from 0.0 - 1.0.
 * Gradient: LERP from black (top left) to white (bottom right).
 * Corners: LERP with black, red, blue, green in their own corners.

Use Random Input Colors
 * If checked, # of Random Colors will be read, and that many colors will be randomly generated and presented to the SOM as input data.

Learning Rate:
 * Learning rate for the SOM.

Iterations:
 * \# of iterations to perform.

Reset:
 * If a setting is changed, reset needs to be clicked to re-initialize the network with the given settings.

Train:
 * Starts the training of the SOM. If gray, a setting has been changed and Reset must be clicked.

Network Status:
 * Shows the status of the network. While training it will show the current iteration of the training phase. Upon completion, it will display the total map error.

## SOM_Image

SOM_Image demonstrates a practical example of the Self-Organizing Map
algorithm. The basic idea, is that upon giving the network a database
of images, the algorithm will 'learn' these images, classifying them
into groups with similar images.

Currently, with a 10x10 network, an good number of pictures to use
is 200-300. This will provide (in an ideal situation) 2-3 pictures per
network node.

![SOM_Image](https://raw.githubusercontent.com/doggan/som-demos/screenshots/screenshots/som_image.jpg "SOM Image")

### Usage

To use this application, click "Browse". Select a folder that contains
the images you wish to use as your database. Currently, the application
will attempt to use all .bmp files within the given directory. Simply
click Reset to calculate the data vectors for all images, and then Train.
(Sample images are provided in the ```NFL_Images``` directory).

Upon successful training, the Network window will be filled with numbers.
These numbers correspond to the # of images associated with each node. Click
the node, and it's associated images will appear in the left window.

### Overview and Options

Windows:
 * Network window:
  * This window shows the # of images currently associated with each network node. Prior to training, all numbers will be 0. Click on a node to show it's associated images.
 * Error Map window:
  * This window will show the error map of the network. Whites represent higher error, while blacks are lower error.
 * Images in Selected Node window:
  * Will display the images in the currently selected node.

Initializations:
 * Random: Fill the network with random values from 0.0 - 1.0.
 * Gradient: LERP from all 0.0 weights (top left) to all 1.0 weights (bottom right).

Learning Rate (2 Phases):
 * Learning rate for the SOM.

Iterations (2 Phases):
 * \# of iterations to perform.

Image Directory:
 * Shows the directory that will be used to read input images from. Use 'Browse' to select a directory.

Reset:
 * If a setting is changed, reset needs to be clicked to re-initialize the network with the given settings.

Train:
 * Starts the training of the SOM. If gray, a setting has been changed and Reset must be clicked.

Network Status:
 * Shows the status of the network. While training it will show the current iteration of the training phase. Upon completion, it will display the total map error.

### Possible Future Enhancements

 * XML file interaction. Instead of re-calculating the data vectors
 for all images each time reset is clicked, it would be more efficient
 to store these vectors in some data file and just read them in.

 * Find Image feature. After the network has been trained, allow the user
 the opportunity to present a new image to the network. Then, return
 to the user images that are similar to this presented image.
