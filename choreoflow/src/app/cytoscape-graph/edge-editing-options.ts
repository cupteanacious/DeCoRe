export const edgeEditingOptions = {
    // A function parameter to get bend point positions, should return positions of bend points
    bendPositionsFunction: function(ele: { data: (arg0: string) => any; }) {
      return ele.data('bendPointPositions');
    },
    // A function parameter to get control point positions, should return positions of control points
    controlPositionsFunction: function(ele: { data: (arg0: string) => any; }) {
      return ele.data('controlPointPositions');
    },
    // A function parameter to set bend point positions
    bendPointPositionsSetterFunction: function(ele: { data: (arg0: string, arg1: any) => void; }, bendPointPositions: any) {
      ele.data('bendPointPositions', bendPointPositions);
    },
    // A function parameter to set bend point positions
    controlPointPositionsSetterFunction: function(ele: { data: (arg0: string, arg1: any) => void; }, controlPointPositions: any) {
      ele.data('controlPointPositions', controlPointPositions);
    },
    // whether to initilize bend and control points on creation of this extension automatically
    initAnchorsAutomatically: true,
    // the classes of those edges that should be ignored
    ignoredClasses: [],
    // whether the bend editing operations are undoable (requires cytoscape-undo-redo.js)
    undoable: false,
    // the size of bend and control point shape is obtained by multipling width of edge with this parameter
    anchorShapeSizeFactor: 3,
    // z-index value of the canvas in which bend points are drawn
    zIndex: 999,
     /*An option that controls the distance (in pixels) within which a bend point is considered near the line segment between 
       its two neighbors and will be automatically removed
       min value = 0 , max value = 20 , values less than 0 are set to 0 and values greater than 20 are set to 20
     */
    bendRemovalSensitivity : 8,
    // title of add bend point menu item (User may need to adjust width of menu items according to length of this option)
    addBendMenuItemTitle: "Add Bend Point",
    // title of remove bend point menu item (User may need to adjust width of menu items according to length of this option)
    removeBendMenuItemTitle: "Remove Bend Point",
    // title of remove all bend points menu item
    removeAllBendMenuItemTitle: "Remove All Bend Points",
    // title of add control point menu item (User may need to adjust width of menu items according to length of this option)
    addControlMenuItemTitle: "Add Control Point",
    // title of remove control point menu item (User may need to adjust width of menu items according to length of this option)
    removeControlMenuItemTitle: "Remove Control Point",
    // title of remove all control points menu item
    removeAllControlMenuItemTitle: "Remove All Control Points",
    // whether 'Remove all bend points' and 'Remove all control points' options should be presented to the user
    enableMultipleAnchorRemovalOption: false,
    // whether the bend and control points can be moved by arrows
    moveSelectedAnchorsOnKeyEvents: function () {
        return true;
    },
    // this function handles reconnection of the edge, if undefined simply connect edge to its new source/target 
    // handleReconnectEdge (newSource.id(), newTarget.id(), edge.data())
    handleReconnectEdge: undefined,
    // this function checks validation of the edge and its new source/target
    validateEdge: function (edge: any, newSource: any, newTarget: any) {
       return 'valid';
    },
    // this function is called if reconnected edge is not valid according to validateEdge function
    actOnUnsuccessfulReconnection: undefined,
    // specifically for edge-editing menu items, whether trailing dividers should be used
    useTrailingDividersAfterContextMenuOptions: false,
    // Enable / disable drag creation of anchor points when there is at least one anchor already on the edge
    enableCreateAnchorOnDrag: true
  };