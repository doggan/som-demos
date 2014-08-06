#####################################################################
# Author   : Shyam M Guthikonda                                     #
# Modified : 11 Dec., 2005                                          #
# Desc     : This is the README file for the SOM_Color application. #
#####################################################################

SOM_Color demonstrates the basic Self-Organizing Map algorithm. It will
attempt to take the given input colors and represent them on a 2-D
network of nodes.

#################
# Key Features: #
#################

Windows: (from left-right, top-bottom).
    Window 01 - Initialization window.
        => This window shows the weight distribution of the network
            at initialization.
    Window 02 - Training window.
        => This window will be updated each frame, showing the current
            state of the SOM network. Initially, it will be a copy of the
            initialization window.
    Window 03 - BMU window.
        => This window will be calculated upon successful training. It will
            display the most commonly used BMU nodes.
    Window 04 - Error Map window.
        => This window will show the error map of the network. Whites
            represent higher error, while blacks are lower error.

Initializations:
    Random: Fill the network with random values from 0.0 - 1.0.
    Gradient: LERP from black (top left) to white (bottom right).
    Corners: LERP with black, red, blue, green in their own corners.
    
Use Random Input Colors
    If checked, # of Random Colors will be read, and that many colors
    will be randomly generated and presented to the SOM as input data.
    
Learning Rate:
    Learning rate for the SOM.
    
Iterations:
    # of iterations to perform.
    
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