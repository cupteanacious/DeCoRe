import * as cytoscape from 'cytoscape';
import {
  DCRChoreography,
  Event,
  Relationship,
  Relation,
  Nesting,
  Marking,
} from './dcr.model';
import { NodeData } from './cytoscape.model';

function createIconElement(iconName: string): HTMLSpanElement {
  const matIcon = document.createElement('mat-icon');
  matIcon.classList.add('mat-icon');
  matIcon.classList.add('notranslate');
  matIcon.classList.add('material-icons');
  matIcon.classList.add('mat-ligature-font');
  matIcon.classList.add('mat-icon-no-color');
  matIcon.setAttribute('role', 'img');
  matIcon.setAttribute('aria-hidden', 'true');
  matIcon.setAttribute('data-mat-icon-type', 'font');
  matIcon.innerText = iconName;
  return matIcon;
}

export function updateMarkings(node: NodeData) {
  const eventContainer = node.dom;
  const markings = node.markings;
  if (!eventContainer || !markings) {
    return;
  }

  const eventDiv = eventContainer.firstChild as HTMLDivElement;
  if (markings.included) {
    eventDiv.classList.remove('excluded-event');
  } else {
    eventDiv.classList.add('excluded-event');
  }

  const pendingIcon = eventContainer.querySelector('.pending-icon');
  if (markings.pending) {
    pendingIcon?.classList.remove('hidden');
  } else {
    pendingIcon?.classList.add('hidden');
  }
  
  const executedIcon = eventContainer.querySelector('.executed-icon');
  if (markings.executed) {
    executedIcon?.classList.remove('hidden');
  } else {
    executedIcon?.classList.add('hidden');
  }
}

export function createEventTemplate(
  nodeData: NodeData,
): HTMLDivElement {
  const initiator: string = nodeData.initiator;
  const action: string = nodeData.action;
  const receivers: string = nodeData.receivers;

  const div = document.createElement('div');

  const eventContainer = document.createElement('div');
  div.appendChild(eventContainer);
  eventContainer.classList.add('event-node');

  const executedIcon = createIconElement('done');
  executedIcon.classList.add('executed-icon');
  eventContainer.appendChild(executedIcon);

  const pendingIcon = createIconElement('priority_high');
  pendingIcon.classList.add('pending-icon');
  eventContainer.appendChild(pendingIcon);

  const initiatorDiv = document.createElement('div');
  initiatorDiv.classList.add('event-initiator');
  initiatorDiv.innerText = initiator;
  eventContainer.appendChild(initiatorDiv);

  const actionDiv = document.createElement('div');
  actionDiv.classList.add('event-action');
  actionDiv.innerText = action;
  eventContainer.appendChild(actionDiv);

  const receiversDiv = document.createElement('div');
  receiversDiv.classList.add('event-receivers');
  receiversDiv.innerText = receivers;
  eventContainer.appendChild(receiversDiv);

  return div;
}

// funtions that converts nodes and edges of the cytoscape graph to the DCR model
export function convertGraphToDCRModel(cy: cytoscape.Core): DCRChoreography {
  const dcrChoreography: DCRChoreography = {
    events: [],
    nestings: [],
    relationships: [],
    markings: [],
  };

  // get non nested nodes without .nesting class from the cytoscape graph
  const nonNestedNodes = cy.nodes().filter((node) => !node.hasClass('nesting'));

  // convert nodes to events
  nonNestedNodes.forEach((node) => {
    const event = convertNodeToEvent(node);
    dcrChoreography.events.push(event);
  });

  // convert nonNestedNodes to markings
  nonNestedNodes.forEach((node) => {
    const data = node.data();
    const marking = {
      eventId: data.id,
      marking: {
        executed: data.markings.executed,
        included: data.markings.included,
        pending: data.markings.pending,
      },
    };
    dcrChoreography.markings.push(marking);
  });

  // get all edges of the cytoscape graph
  const edges = cy.edges();

  // convert edges to relationships
  edges.forEach((edge) => {
    const relationship = convertEdgeToRelationship(edge);
    dcrChoreography.relationships.push(relationship);
  });

  // get all nested nodes with .nesting class from the cytoscape graph
  const nestedNodes = cy.nodes().filter((node) => node.hasClass('nesting'));

  // convert nested nodes to nestings
  nestedNodes.forEach((node) => {
    const nesting = convertNodeToNesting(node);
    dcrChoreography.nestings.push(nesting);
  });

  // remove duplicate nestings or any nesting that is a child of another nesting
  dcrChoreography.nestings = removeDuplicateNestings(dcrChoreography.nestings);

  return dcrChoreography;
}

// function that converts a node to an event
function convertNodeToEvent(node: cytoscape.NodeSingular): Event {
  const data = node.data();
  const event: Event = {
    id: data.id,
    initiator: data.initiator,
    action: data.action,
    receivers: (data.receivers as string)
      .split(',')
      .map((receiver) => receiver.trim()),
  };
  return event;
}

// function that converts an edge to a relationship
function convertEdgeToRelationship(edge: cytoscape.EdgeSingular): Relationship {
  const relationship: Relationship = {
    from: edge.source().id(),
    to: edge.target().id(),
    relations: [],
  };
  const relation: Relation = {
    relationType: edge.data('relationType'),
  };
  relationship.relations.push(relation);
  return relationship;
}

// function that converts a node to a nesting
function convertNodeToNesting(node: cytoscape.NodeSingular): Nesting {
  const nesting: Nesting = {
    id: node.id(),
    events: [],
    childNestings: [],
  };
  // get all child nodes of the nesting node
  const childNodes = node.children();
  childNodes.forEach((childNode) => {
    // if child node is a nesting node, convert it to a nesting
    if (childNode.hasClass('nesting')) {
      const childNesting = convertNodeToNesting(childNode);
      nesting.childNestings.push(childNesting);
    } else {
      // if child node is not a nesting node, add it to the events of the nesting
      nesting.events.push(childNode.id());
    }
  });
  return nesting;
}

// function that removes duplicate nestings or any nesting that is a child of another nesting
function removeDuplicateNestings(nestings: Nesting[]): Nesting[] {
  const uniqueNestings: Nesting[] = [];
  nestings.forEach((nesting) => {
    // if the nesting is not a child of another nesting, add it to the unique nestings
    if (
      !isChildNesting(
        nesting,
        nestings.filter((n) => n.id !== nesting.id)
      )
    ) {
      uniqueNestings.push(nesting);
    }
  });
  return uniqueNestings;
}

// function that checks if a nesting is a child of another nesting
function isChildNesting(nesting: Nesting, nestings: Nesting[]): boolean {
  let isChild = false;
  nestings.forEach((n) => {
    // if the nesting is a child of another nesting, set isChild to true
    if (n.id === nesting.id) {
      isChild = true;
    } else {
      // if the nesting is not a child of another nesting, check if it is a child of the child nestings of the other nesting
      isChild = isChildNesting(nesting, n.childNestings);
    }
  });
  return isChild;
}

function clearCY(cy: cytoscape.Core): void {
  const elements = cy.elements();
  elements.forEach((element) => {
    const dom = element.data('dom');
      if (dom) {
        dom.remove();
      }
  });
  cy.remove(elements);
}

export function convertDCRModelToGraph(
  cy: cytoscape.Core,
  dcrChoreography: DCRChoreography
): void {
  // remove all nodes and edges from the cytoscape graph
  clearCY(cy);

  const events = dcrChoreography.events;
  const nestings = dcrChoreography.nestings;
  const relationships = dcrChoreography.relationships;
  const markings = dcrChoreography.markings;

  // convert events to nodes
  events.forEach((event) => {
    convertEventToNode(cy, event);
  });

  // convert nestings to nodes
  nestings.forEach((nesting) => {
    convertNestingToNode(cy, nesting);
  });

  // convert relationships to edges
  relationships.forEach((relationship) => {
    convertRelationshipToEdge(cy, relationship);
  });

  // convert markings to markings
  markings.forEach((marking) => {
    convertMarkingToMarking(cy, marking);
  });
}

// function that converts an event to a node
function convertEventToNode(cy: cytoscape.Core, event: Event): void {
  const receivers = event.receivers.join(', ');
  const nodeData: NodeData = {
    id: event.id,
    initiator: event.initiator,
    action: event.action,
    receivers,
    markings: {
      executed: false,
      included: false,
      pending: false,
    },
  }

  nodeData.dom = createEventTemplate(nodeData);
  updateMarkings(nodeData);
  const node = cy.add({
    group: 'nodes',
    data: nodeData,
  });
  node.addClass('event');
}

// function that converts a nesting to a node
function convertNestingToNode(cy: cytoscape.Core, nesting: Nesting, parentNesting?: string): void {
  const node = cy.add({
    group: 'nodes',
    data: {
      id: nesting.id,
    },
    classes: 'nesting',
  });
  // convert events to nodes
  nesting.events.forEach((eventId) => {
    const eventNode = cy.getElementById(eventId);
    eventNode.move({ parent: nesting.id });
  });
  // convert child nestings to nodes
  nesting.childNestings.forEach((childNesting) => {
    convertNestingToNode(cy, childNesting, nesting.id);
  });
  // if the nesting has a parent nesting, move it to the parent nesting
  if (parentNesting) {
    node.move({ parent: parentNesting });
  }
}

// function that converts a relationship to an edge
function convertRelationshipToEdge(
  cy: cytoscape.Core,
  relationship: Relationship
): void {
  relationship.relations.forEach((relation) => {
    const relationType = relation.relationType.toString();
  
    const edge = cy.add({
      group: 'edges',
      data: {
        source: relationship.from,
        target: relationship.to,
        relationType,
      },
    });
  
    // style the loop edge
    if (relationship.from === relationship.to) {
      edge.style({
        'curve-style': 'unbundled-bezier',
        'loop-direction': '0deg',
        'loop-sweep': '-45deg',
        'control-point-step-size': '100px',
      });
    }
  });
}

// function that converts a marking to a marking
function convertMarkingToMarking(cy: cytoscape.Core, marking: Marking): void {
  const node = cy.getElementById(marking.eventId);
  node.data('markings', marking.marking);
  updateMarkings(node.data());
}
