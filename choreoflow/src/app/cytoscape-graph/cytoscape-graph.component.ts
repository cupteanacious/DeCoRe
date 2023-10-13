import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
// import * as cytoscape from 'cytoscape';
// import * as cytoscape from 'cytoscape';
import {
  convertGraphToDCRModel,
  convertDCRModelToGraph,
  createEventTemplate,
  updateMarkings,
} from './conversion.utils';
import { DCRChoreography, getAllNestingIds } from './dcr.model';
import { NodeData } from './cytoscape.model';
import { edgeEditingOptions } from './edge-editing-options';
// import * as contextMenus from 'cytoscape-context-menus';
var cytoscape = require('cytoscape');
var jquery = require('jquery');
var konva = require('konva');
var edgeEditing = require('cytoscape-edge-editing');

edgeEditing(cytoscape, jquery, konva); // register extension
cytoscape.use(require('cytoscape-dom-node'));

@Component({
  selector: 'app-cytoscape-graph',
  templateUrl: './cytoscape-graph.component.html',
  styleUrls: ['./cytoscape-graph.component.scss'],
})
export class CytoscapeGraphComponent implements OnInit {
  @ViewChild('cy', { static: true }) cyElement!: ElementRef;
  private cy!: cytoscape.Core;

  // Form values
  initiator: string = '';
  action: string = '';
  receivers: string = '';
  relationType: string = '';
  showForm: boolean = false;
  nestingId: string = '';
  selected: NodeData | undefined = undefined;

  // Cytoscape graph values
  selectedNesting: string = '';
  nestingIds: string[] = [];
  selectedEvents: cytoscape.CollectionReturnValue | undefined;
  selectedRelationships: cytoscape.CollectionReturnValue | undefined;

  constructor(private el: ElementRef) {}

  ngOnInit(): void {
    let div = document.createElement('div');
    div.innerHTML = `node 11`;
    div.style.width = `${Math.floor(Math.random() * 40) + 60}px`;
    div.style.height = `${Math.floor(Math.random() * 30) + 50}px`;
    const defaultNodes: cytoscape.NodeDefinition[] = this.initialEvents();
    const cytoscapeStyles: cytoscape.Stylesheet[] = [
      // Node styles
      // {
      //   selector: 'node',
      //   style: {
      //     label: 'data(label)',
      //     'text-valign': 'center',
      //     'text-halign': 'center',
      //     'text-wrap': 'wrap',
      //     'background-color': '#11479e',
      //     color: '#fff',
      //     width: '200',
      //     height: '100',
      //     shape: 'round-rectangle',
      //     'border-color': '#000',
      //     'border-width': '1',
      //   },
      // },
      // ... other styles
      {
        selector: ':selected',
        style: {
          'background-color': '#ff4081',
          'line-color': '#ff4081',
          'target-arrow-color': '#ff4081',
          'source-arrow-color': '#ff4081',
          'border-color': '#ff4081',
          'border-width': '5',
          'border-style': 'solid',
        },
      },
      // {
      //   selector: ':selected.nesting', // Select compound nodes when selected
      //   style: {
      //     'border-width': 3,
      //     'border-color': 'red',
      //   },
      // },
      {
        selector: 'node.nesting',
        style: {
          label: 'data(id)',
          shape: 'round-rectangle',
          'background-color': 'white',
          'background-opacity': 0.5,
          'text-valign': 'bottom',
          'text-halign': 'center',
          'border-color': 'black',
          'padding-bottom': '20px',
          'padding-left': '20px',
          'padding-right': '20px',
          'padding-top': '20px',
          'border-width': '1',
          'border-style': 'dashed',
        },
      },
      {
        selector: ':parent:selected',
        style: {
          'background-color': '#ff4081',
          'line-color': '#ff4081',
          'target-arrow-color': '#ff4081',
          'source-arrow-color': '#ff4081',
          'border-color': '#ff4081',
          'border-width': '1',
          'border-style': 'solid',
        },
      },
      {
        selector: 'edge',
        style: {
          width: 1,
          // 'curve-style': 'bezier',
          // 'curve-style': 'taxi',
          // 'curve-style': 'segments',
          'curve-style': 'unbundled-bezier',
          'line-color': 'black',
          'target-arrow-color': 'black',
        },
      },
      {
        selector: 'edge[relationType = "Response"]',
        style: {
          'target-arrow-color': 'black',
          'target-arrow-shape': 'triangle',
          'target-arrow-fill': 'filled',
          'source-arrow-color': 'black',
          'source-arrow-shape': 'circle',
          'source-arrow-fill': 'filled',
        },
      },
      {
        selector: 'edge[relationType = "Condition"]',
        style: {
          'target-arrow-color': 'black',
          'target-arrow-shape': 'circle-triangle',
          'target-arrow-fill': 'filled',
        },
      },
      {
        selector: 'edge[relationType = "Milestone"]',
        style: {
          'target-arrow-color': 'black',
          'target-arrow-shape': 'diamond',
          'target-arrow-fill': 'hollow',
        },
      },
      {
        selector: 'edge[relationType = "Exclusion"]',
        style: {
          'target-label': '%',
          'target-text-margin-y': -10,
          'target-text-offset': 10,
        },
      },
      {
        selector: 'edge[relationType = "Inclusion"]',
        style: {
          'target-label': '+',
          'target-text-margin-y': -10,
          'target-text-offset': 10,
        },
      },
      // ... define styles for other relationship types
    ];

    // Add the styles when initializing the Cytoscape instance
    this.cy = cytoscape({
      container: this.cyElement.nativeElement,
      elements: { nodes: defaultNodes, edges: [] },
      style: cytoscapeStyles,
      layout: this.getGridLayout(),
      wheelSensitivity: 0.1,
    });
    (this.cy as any).domNode();
    (this.cy as any).edgeEditing(edgeEditingOptions);

    this.cy.on('select', '*', this.onSelect.bind(this));
    this.cy.on('unselect', '*', this.onSelect.bind(this));
    this.cy.on('tap', '*:selected', (event) => {
      setTimeout(() => {
        const tapped = event.target;
        if (tapped.selected()) {
          tapped.unselect();
          this.getSelectedNodes();
          this.getSelectedEdges();
        }
      }, 0);
    });
    this.cy.reset();
  }

  private fit() {
    setTimeout(() => {
      this.cy.fit(this.cy.$('node'));
    }, 1000);
  }

  private getGridLayout() {
    let options = {
      name: 'grid',

      fit: true, // whether to fit the viewport to the graph
      padding: 30, // padding used on fit
      boundingBox: undefined, // constrain layout bounds; { x1, y1, x2, y2 } or { x1, y1, w, h }
      avoidOverlap: true, // prevents node overlap, may overflow boundingBox if not enough space
      avoidOverlapPadding: 10, // extra spacing around nodes when avoidOverlap: true
      nodeDimensionsIncludeLabels: false, // Excludes the label when calculating node bounding boxes for the layout algorithm
      spacingFactor: undefined, // Applies a multiplicative factor (>0) to expand or compress the overall area that the nodes take up
      condense: false, // uses all available space on false, uses minimal space on true
      rows: undefined, // force num of rows in the grid
      cols: undefined, // force num of columns in the grid
      // position: function( node ){}, // returns { row, col } for element
      sort: undefined, // a sorting function to order the nodes; e.g. function(a, b){ return a.data('weight') - b.data('weight') }
      animate: false, // whether to transition the node positions
      animationDuration: 500, // duration of animation in ms if enabled
      animationEasing: undefined, // easing of animation if enabled
      // animateFilter: function ( node, i ){ return true; }, // a function that determines whether the node should be animated.  All nodes animated by default on animate enabled.  Non-animated nodes are positioned immediately when the layout starts
      ready: undefined, // callback on layoutready
      stop: undefined, // callback on layoutstop
      // transform: function (node, position ){ return position; } // transform a given node position. Useful for changing flow direction in discrete layouts
    };
    return options;
  }

  onSelect(event: any): void {
    this.getSelectedNodes();
    this.getSelectedEdges();
  }

  getSelectedEdges(): void {
    this.selectedRelationships = this.cy.$('edge:selected');
    console.log('selected relationship: ', this.selectedRelationships);
  }

  getSelectedNodes(): void {
    this.selectedEvents = this.cy.$('node:selected');
    this.getFirstSelectedNodeData();
    console.log('selected nodes: ', this.selectedEvents);
  }

  //get data of first selected node
  getFirstSelectedNodeData(): void {
    if (this.selectedEvents?.size() === 1) {
      this.selected = this.selectedEvents?.first().data();
      console.log('selected node data: ', this.selected);
    } else {
      this.selected = undefined;
    }
  }

  getAllNestings(): void {
    this.nestingIds = this.cy
      .$('.nesting')
      .toArray()
      .map((node: cytoscape.SingularElementArgument) => node.id());
    console.log('nestings: ', this.nestingIds);
  }

  initialEvents() {
    const node1: NodeData = {
      id: 'initiator1action1receiver1receiver2',
      initiator: 'initiator1',
      action: 'action1',
      receivers: 'receiver1, receiver2',
      markings: {
        pending: false,
        executed: false,
        included: true,
      },
    };
    (node1.dom = createEventTemplate(node1)), updateMarkings(node1);

    const node2: NodeData = {
      id: 'initiator2action2receiver1receiver2',
      initiator: 'initiator2',
      action: 'action2',
      receivers: 'receiver1, receiver2',
      markings: {
        pending: false,
        executed: false,
        included: true,
      },
    };
    (node2.dom = createEventTemplate(node2)), updateMarkings(node2);

    const node3: NodeData = {
      id: 'initiator3action3receiver1receiver2',
      initiator: 'initiator3',
      action: 'action3',
      receivers: 'receiver1, receiver2',
      markings: {
        pending: false,
        executed: false,
        included: true,
      },
    };
    (node3.dom = createEventTemplate(node3)), updateMarkings(node3);

    const events: {data: NodeData;}[] = [
      {
        data: node1
      },
      {
        data: node2
      },
      {
        data: node3
      },
    ];
    return events;
  }

  addEvent(initiator: string, action: string, receivers: string): void {
    // Create a unique ID from initiator, action, and receivers
    const id =
      initiator +
      action +
      receivers
        .split(',')
        .map((r) => r.trim())
        .join('');
    
    const node: NodeData = {
      id,
      initiator,
      action,
      receivers,
      markings: {
        pending: false,
        executed: false,
        included: true,
      }
    }

    node.dom = createEventTemplate(node);
    updateMarkings(node);
    // Add the event to the Cytoscape graph
    this.cy.add({
      group: 'nodes',
      data: node,
      position: { x: 200, y: 200 },
    });

    // Reset form values
    this.initiator = '';
    this.action = '';
    this.receivers = '';
    this.fit();
  }

  createRelationship(relationType: string): void {
    if (
      !this.selectedEvents ||
      this.selectedEvents.size() > 2 ||
      this.selectedEvents.size() === 0
    ) {
      return;
    }
    let source = this.selectedEvents.first().id();
    let target = this.selectedEvents.last().id();

    const edge = this.cy.add({
      group: 'edges',
      data: { source, target, relationType },
    });

    // style the loop edge
    if (source === target) {
      edge.style({
        'curve-style': 'unbundled-bezier',
        'loop-direction': '0deg',
        'loop-sweep': '-45deg',
        // 'source-endpoint': '25% -50%',
        'control-point-step-size': '100px',
        // 'target-endpoint': '45% 45%',
      });
    }

    this.selectedEvents = undefined;
    this.relationType = '';
  }

  removeNode(nodeId: string): void {
    console.log('remove node: ', nodeId);
    this.cy.getElementById(nodeId).remove();
  }

  addNesting(nestingId: string): void {
    // Add the nesting (compound node) to the Cytoscape graph
    this.cy.add({
      group: 'nodes',
      data: { id: nestingId },
      position: { x: 200, y: 200 },
      classes: 'nesting',
    });
    this.getAllNestings();
    console.log('nestings: ', this.nestingIds);

    // Reset the nesting name
    this.nestingId = '';
  }

  removeNesting(nestingId: string): void {
    this.cy.getElementById(nestingId).remove();
  }

  getNestingChildren(nestingId: string): any[] {
    return this.cy
      .getElementById(nestingId)
      .children()
      .map((node) => node.data());
  }

  addToNesting(nestingId: string): void {
    this.selectedEvents?.forEach((event) => {
      event.move({ parent: nestingId });
    });
  }

  removeFromNesting(): void {
    this.selectedEvents?.forEach((event) => {
      event.move({ parent: null });
    });
  }

  anyRemoveableEventSelected(): boolean {
    return (
      !!this.selectedEvents &&
      this.selectedEvents.size() > 0 &&
      this.selectedEvents.filter((event) => !!event.data('parent')).size() > 0
    );
  }

  removeSelectedRelationships(): void {
    this.selectedRelationships?.remove();
  }

  reverseRelationship(): void {
    this.selectedRelationships?.forEach((relationship) => {
      const source = relationship.data('source');
      const target = relationship.data('target');
      const relationType = relationship.data('relationType');
      const id = target + relationType + source;
      this.cy.remove(relationship);
      this.cy.add({
        group: 'edges',
        data: { id, source: target, target: source, relationType },
      });
    });
  }

  removeEvents(
    events:
      | cytoscape.CollectionReturnValue
      | cytoscape.NodeCollection
      | undefined
  ): void {
    if (!events) {
      return;
    }

    events.forEach((event) => {
      // if the event is a compound node, remove all its children
      if (event.isParent()) {
        this.removeEvents(event.children());
      }

      const dom = event.data('dom');
      if (dom) {
        dom.remove();
      }
      this.cy.remove(event);
    });

    this.cy.forceRender();
    this.selectedEvents = undefined;
    this.selected = undefined;
  }

  convertToJSON(): void {
    const dcr: DCRChoreography = convertGraphToDCRModel(this.cy);
    this.downloadObjectAsJson(dcr, 'graph');
  }

  downloadObjectAsJson(exportObj: Object, exportName: string) {
    var dataStr =
      'data:text/json;charset=utf-8,' +
      encodeURIComponent(JSON.stringify(exportObj));
    var downloadAnchorNode = document.createElement('a');
    downloadAnchorNode.setAttribute('href', dataStr);
    downloadAnchorNode.setAttribute('download', exportName + '.json');
    document.body.appendChild(downloadAnchorNode); // required for firefox
    downloadAnchorNode.click();
    downloadAnchorNode.remove();
  }

  public uploadJSON() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = (e) => {
          const contents = e.target?.result;
          if (typeof contents === 'string') {
            const dcr = JSON.parse(contents) as DCRChoreography;
            convertDCRModelToGraph(this.cy, dcr);
            this.nestingIds = getAllNestingIds(dcr.nestings);
            this.cy.layout({ name: 'grid' }).run();
            this.cy.fit();
          }
        };
        reader.readAsText(file);
      }
    };
    input.click();
  }

  public updateMarkingsHTML(node: NodeData) {
    updateMarkings(node);
  }
}
