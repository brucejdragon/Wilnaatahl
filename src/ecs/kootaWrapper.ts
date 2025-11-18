import {
  IEntity,
  IQueryResult$1 as IQueryResult,
  IRelation$1 as IRelation,
  ITagTrait,
  ITrait,
  ITraitFactory,
  IValueTrait$1 as IValueTrait,
  IWorld,
  QueryOperator,
  QueryOperator_Visit,
  TraitSpec,
  TraitSpec_Map,
} from "../generated/ECS/Types";
import { Option } from "../generated/fable_modules/fable-library-ts.4.27.0/Option";
import {
  AoSFactory,
  ConfigurableTrait,
  createWorld as createKootaWorld,
  Entity,
  InstancesFromParameters,
  Not,
  Or,
  QueryParameter,
  QueryResult,
  relation,
  Relation,
  Schema,
  TagTrait,
  trait,
  Trait,
  TraitValue,
  World,
} from "koota";

type KootaData<T> = T extends Schema ? T : never;
type KootaQueryParameters<S> = S extends QueryParameter[] ? S : [];

class WrappedTagTrait implements ITagTrait {
  readonly IsTag;
  constructor(public readonly trait: TagTrait) {
    this.IsTag = true;
  }
}

class WrappedValueTrait<T> implements IValueTrait<T> {
  readonly IsTag;
  constructor(public readonly trait: Trait<KootaData<T>>) {
    this.IsTag = false;
  }
}

function unwrapValueTrait<T>(traitWrapper: IValueTrait<T>): Trait<KootaData<T>> {
  return (traitWrapper as WrappedValueTrait<T>).trait;
}

export function unwrapTrait<T>(traitWrapper: ITrait): Trait<any> {
  const traitImpl = traitWrapper.IsTag
    ? (traitWrapper as WrappedTagTrait)
    : (traitWrapper as WrappedValueTrait<T>);
  return traitImpl.trait;
}

class WrappedEntity implements IEntity {
  constructor(public readonly entity: Entity) {}

  private static unwrapRelation<TTrait extends ITrait>(r: IRelation<TTrait>): Relation<Trait<any>> {
    const wrapper = r.IsTag
      ? (r as unknown as WrappedTagRelation)
      : (r as unknown as WrappedValueRelation<TTrait>);
    return wrapper.rel;
  }

  static wrap(entity: Entity | undefined) {
    if (!entity) {
      return undefined;
    }

    return new WrappedEntity(entity);
  }

  Add(someTrait: ITrait): void {
    this.entity.add(unwrapTrait(someTrait));
  }

  Destroy(): void {
    this.entity.destroy();
  }

  Get<T>(valueTrait: IValueTrait<T>): Option<T> {
    return this.entity.get(unwrapValueTrait(valueTrait));
  }

  Has(someTrait: ITrait): boolean {
    return this.entity.has(unwrapTrait(someTrait));
  }

  Remove(someTrait: ITrait): void {
    this.entity.remove(unwrapTrait(someTrait));
  }

  Set<T>(valueTrait: IValueTrait<T>, value: T): void {
    const valueToSet = value as TraitValue<KootaData<T>>;
    this.entity.set(unwrapValueTrait(valueTrait), valueToSet);
  }

  TargetFor<TTrait extends ITrait>(relation: IRelation<TTrait>): Option<IEntity> {
    return WrappedEntity.wrap(this.entity.targetFor(WrappedEntity.unwrapRelation(relation)));
  }

  TargetsFor<TTrait extends ITrait>(relation: IRelation<TTrait>): Iterable<IEntity> {
    const result = this.entity.targetsFor(WrappedEntity.unwrapRelation(relation));
    return result.map((foundEntity) => new WrappedEntity(foundEntity));
  }
}

class WrappedValueRelation<T> implements IRelation<IValueTrait<T>> {
  readonly IsTag: boolean; // For type testing purposes.
  constructor(public readonly rel: Relation<Trait<KootaData<T>>>) {
    this.IsTag = false;
  }
  On(entity: IEntity): IValueTrait<T> {
    return new WrappedValueTrait<T>(this.rel((entity as WrappedEntity).entity));
  }
  Wildcard(): IValueTrait<T> {
    return new WrappedValueTrait<T>(this.rel("*"));
  }
}

class WrappedTagRelation implements IRelation<ITagTrait> {
  readonly IsTag: boolean; // For type testing purposes.
  constructor(public readonly rel: Relation<Trait<Schema>>) {
    this.IsTag = true;
  }
  On(entity: IEntity): ITagTrait {
    return new WrappedTagTrait(this.rel((entity as WrappedEntity).entity) as TagTrait);
  }
  Wildcard(): ITagTrait {
    return new WrappedTagTrait(this.rel("*") as TagTrait);
  }
}

export function createTraitFactory(): ITraitFactory {
  function Relation(exclusive: boolean): IRelation<ITagTrait> {
    const rel = exclusive ? relation({ exclusive: true }) : relation();
    return new WrappedTagRelation(rel);
  }

  function RelationWith<T>(exclusive: boolean, store: T): IRelation<IValueTrait<T>> {
    const typedStore = store as KootaData<T>;
    const rel = exclusive
      ? relation({ exclusive: true, store: typedStore })
      : relation({ store: typedStore });
    return new WrappedValueRelation<T>(rel);
  }

  function TagTrait(): ITagTrait {
    return new WrappedTagTrait(trait());
  }

  function TraitWith<T>(value: T): IValueTrait<T> {
    const traitDef = trait(value as KootaData<T>) as Trait<KootaData<T>>;
    return new WrappedValueTrait<T>(traitDef);
  }

  function TraitWithRef<T>(valueFactory: () => T): IValueTrait<T> {
    const f = valueFactory as T extends Schema ? AoSFactory : never;
    const traitDef = trait(f) as Trait<KootaData<T>>;
    return new WrappedValueTrait<T>(traitDef);
  }

  return {
    Relation,
    RelationWith,
    TagTrait,
    TraitWith,
    TraitWithRef,
  };
}

export class WrappedWorld implements IWorld {
  readonly world: World;

  constructor() {
    this.world = createKootaWorld();
  }

  private static unwrapQueryOperators(ops: QueryOperator[]) {
    return ops.map((op) =>
      QueryOperator_Visit(
        unwrapTrait,
        (ts) => Not(...ts),
        (ts) => Or(...ts),
        op
      )
    );
  }

  Get<T>(valueTrait: IValueTrait<T>): Option<T> {
    return this.world.get(unwrapValueTrait(valueTrait));
  }

  Has(someTrait: ITrait): boolean {
    return this.world.has(unwrapTrait(someTrait));
  }

  Query(...where: QueryOperator[]): IQueryResult<void>;
  Query<T>(traits: IValueTrait<T>, ...where: QueryOperator[]): IQueryResult<T>;
  Query<T, U>(
    traits: [IValueTrait<T>, IValueTrait<U>],
    ...where: QueryOperator[]
  ): IQueryResult<[T, U]>;
  Query<T, U>(traits?: unknown, ...where: QueryOperator[]): IQueryResult<any> {
    const queryParameters = WrappedWorld.unwrapQueryOperators(where);

    function wrapQueryResult<S>(result: QueryResult<KootaQueryParameters<S>>): IQueryResult<S> {
      function UpdateEach(callback: (state: [S, IEntity]) => void): void {
        result.updateEach(
          (state: InstancesFromParameters<KootaQueryParameters<S>>, entity: Entity) => {
            // The type assertion below should be ok. I worked pretty hard to make sure the types line up here...
            callback([state as S, new WrappedEntity(entity)]);
          }
        );
      }

      return { Select: () => wrapQueryResult<void>(result.select()), UpdateEach };
    }

    if (traits && Array.isArray(traits)) {
      const [first, second] = traits as [IValueTrait<T>, IValueTrait<U>];
      const result = this.world.query(
        unwrapValueTrait(first),
        unwrapValueTrait(second),
        ...queryParameters
      );
      return wrapQueryResult(result);
    } else if (traits) {
      const valueTrait = traits as IValueTrait<T>;
      const result = this.world.query(unwrapValueTrait(valueTrait), ...queryParameters);
      return wrapQueryResult(result);
    } else {
      const result = this.world.query(...queryParameters);
      return wrapQueryResult(result);
    }
  }

  QueryFirst(...where: QueryOperator[]): Option<IEntity> {
    const queryParameters = WrappedWorld.unwrapQueryOperators(where);
    return WrappedEntity.wrap(this.world.queryFirst(...queryParameters));
  }

  Remove(someTrait: ITrait): void {
    return this.world.remove(unwrapTrait(someTrait));
  }

  Spawn(...traits: TraitSpec[]): IEntity {
    function unwrapTraitSpec(c: TraitSpec): ConfigurableTrait<Trait<any>> {
      return TraitSpec_Map(
        unwrapTrait,
        ([traitWrapper, value]) =>
          [unwrapTrait(traitWrapper), value] as ConfigurableTrait<Trait<any>>,
        c
      );
    }

    const e = this.world.spawn(...traits.map(unwrapTraitSpec));
    return new WrappedEntity(e);
  }
}

export function createWorld() {
  return new WrappedWorld();
}
