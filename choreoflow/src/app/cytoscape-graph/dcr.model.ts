export interface DCRChoreography {
  events: Event[];
  nestings: Nesting[];
  relationships: Relationship[];
  markings: Marking[];
}

export interface Event {
  id: string;
  initiator: string;
  action: string;
  receivers: string[];
}

export interface Nesting {
  id: string;
  events: string[];
  childNestings: Nesting[];
}

export interface Relationship {
  from: string;
  to: string;
  relations: Relation[];
}

export interface Relation {
  relationType: RelationshipType;
}

export interface Marking {
  eventId: string;
  marking: MarkingType;
}

export enum RelationshipType {
  Response = 'Response',
  Condition = 'Condition',
  Milestone = 'Milestone',
  Exclusion = 'Exclusion',
  Inclusion = 'Inclusion',
}

export interface MarkingType {
  executed: boolean;
  included: boolean;
  pending: boolean;
}

export function getAllNestingIds(nestings: Nesting[]): string[] {
  const nestingIds: string[] = [];
  nestings.forEach((nesting) => {
    nestingIds.push(nesting.id);
    nestingIds.push(...getAllNestingIds(nesting.childNestings));
  });
  return nestingIds;
}