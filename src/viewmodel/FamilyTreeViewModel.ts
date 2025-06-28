import { Person } from "../model/Person";

export type Node = {
  id: string;
  position: [number, number, number];
  children: string[];
  person?: Person;
};