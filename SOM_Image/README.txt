#####################################################################
# Author   : Shyam M Guthikonda                                     #
# Modified : 11 Dec., 2005                                          #
# Desc     : This is the README file for the SOM_Image application. #
#####################################################################

SOM_Image demonstrates a practical example of the Self-Organizing Map
algorithm. The basic idea, is that upon giving the network a database
of images, the algorithm will 'learn' these images, classifying them
into groups with similar images.

Currently, with a 10x10 network, an good number of pictures to use
is 200-300. This will provide (in an ideal situation) 2-3 pictures per
network node.

##########
# Usage: #
##########

To use this application, click "Browse". Select a folder that contains
the images you wish to use as your database. Currently, the application
will attempt to use all .bmp files within the given directory. Simply
click Reset to calculate the data vectors for all images, and then Train.

Upon successful training, the Network window will be filled with numbers.
These numbers correspond to the # of images associated with each node. Click
the node, and it's associated images will appear in the left window.

#################
# Key Features: #
#################

Windows:
    Network window:
        => This window shows the # of images currently associated with each
            network node. Prior to training, all numbers will be 0. Click on
            a node to show it's associated images.
    Error Map window:
        => This window will show the error map of the network. Whites
            represent higher error, while blacks are lower error.
    Images in Selected Node window:
        => Will display the images in the currently selected node.

Initializations:
    Random: Fill the network with random values from 0.0 - 1.0.
    Gradient: LERP from all 0.0 weights (top left) to all 1.0
              weights (bottom right).
    
Learning Rate (2 Phases):
    Learning rate for the SOM.
    
Iterations (2 Phases):
    # of iterations to perform.
    
Image Directory:
    Shows the directory that will be used to read input images from. Use
    'Browse' to select a directory.
    
Reset:
    If a setting is changed, reset needs to be clicked to re-initialize
    the network with the given settings.

Train:
    Starts the training of the SOM. If gray, a setting has been changed and
    Reset must be clicked.
    
Network Status:
    Shows the status of the network. While training it will show the current
    iteration of the training phase. Upon completion, it will display the
    total map error.
    
##########################
# Possible Enhancements: #
##########################

- XML file interaction. Instead of re-calculating the data vectors
    for all images each time reset is clicked, it would be more efficient
    to store these vectors in some data file and just read them in.
    
- Find Image feature. After the network has been trained, allow the user
    the opportunity to present a new image to the network. Then, return
    to the user images that are similar to this presented image.