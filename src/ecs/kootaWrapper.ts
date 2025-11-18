import {
  EntityId,
  IEntityOperations,
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

type KootaSchema<T> = T extends Schema ? T : never;
type KootaValueTrait<T> = Trait<KootaSchema<T>>;
type KootaQueryParameters<S> = S extends QueryParameter[] ? S : [];

type WrappedTrait<TKootaTrait extends Trait<any>> = { IsTag: boolean; trait: TKootaTrait };
type WrappedTagTrait = WrappedTrait<TagTrait>;
type WrappedValueTrait<T> = WrappedTrait<KootaValueTrait<T>>;

type WrappedRelation<TKootaTrait extends Trait<any>, TTrait extends ITrait> = IRelation<TTrait> & {
  IsTag: boolean;
  rel: Relation<TKootaTrait>;
};

type WrappedTagRelation = WrappedRelation<TagTrait, ITagTrait>;
type WrappedValueRelation<T> = WrappedRelation<KootaValueTrait<T>, IValueTrait<T>>;

// The $wrapper symbol allows us to cache wrappers directly on Koota objects without conflicting
// with present or future Koota properties. While it's sketchy to mutate objects coming from Koota,
// we do it so they aren't allocated repeatedly, which will be important during rendering.
const $wrapper = Symbol.for("wilnaatahl.kootaWrapper");

type WithWrapper<T, W> = T & { [$wrapper]: W };

// Returns the hidden $wrapper property of the given object, creating and attaching it to the object
// with the given factory function if it doesn't already exist. The factory function will be passed
// the given object, but if it closes over it already, it's safe to ignore the parameter.
function getOrCreateWrapper<T extends object, TWrapper>(
  obj: T,
  createWrapper: (o: T) => TWrapper
): TWrapper {
  const objWithWrapper: WithWrapper<T, TWrapper> =
    $wrapper in obj
      ? (obj as WithWrapper<T, TWrapper>)
      : Object.assign(obj, {
          [$wrapper]: createWrapper(obj),
        });

  return objWithWrapper[$wrapper];
}

function validateWrappedTrait(trait: ITrait, method: string) {
  if (!("trait" in trait)) {
    throw new Error(`Invalid ITrait implementation passed to ${method}().`);
  }
}

export function toKootaTrait<T>(trait: ITrait): Trait<any> {
  validateWrappedTrait(trait, "toKootaTrait");
  const traitWrapper = trait.IsTag ? (trait as WrappedTagTrait) : (trait as WrappedValueTrait<T>);
  return traitWrapper.trait;
}

// The strongly-typed version is necessary to get the right typing of Get/Set methods
// on the Entity and World wrappers.
function toKootaValueTrait<T>(trait: IValueTrait<T>): KootaValueTrait<T> {
  validateWrappedTrait(trait, "toKootaValueTrait");
  return (trait as WrappedValueTrait<T>).trait;
}

export function createEntityOperations(): IEntityOperations {
  function toKootaRelation<TTrait extends ITrait>(r: IRelation<TTrait>): Relation<Trait<any>> {
    if (!("rel" in r)) {
      throw new Error("Invalid IRelation implementation passed to toKootaRelation().");
    }
    const wrapper = r.IsTag ? (r as WrappedTagRelation) : (r as WrappedValueRelation<TTrait>);
    return wrapper.rel;
  }

  function Add(someTrait: ITrait, entity: EntityId): void {
    (entity as Entity).add(toKootaTrait(someTrait));
  }

  function Destroy(entity: EntityId): void {
    (entity as Entity).destroy();
  }

  function Get<T>(valueTrait: IValueTrait<T>, entity: EntityId): Option<T> {
    return (entity as Entity).get(toKootaValueTrait(valueTrait));
  }

  function Has(someTrait: ITrait, entity: EntityId): boolean {
    return (entity as Entity).has(toKootaTrait(someTrait));
  }

  function Remove(someTrait: ITrait, entity: EntityId): void {
    (entity as Entity).remove(toKootaTrait(someTrait));
  }

  function Set<T>(valueTrait: IValueTrait<T>, value: T, entity: EntityId): void {
    const valueToSet = value as TraitValue<KootaSchema<T>>;
    (entity as Entity).set(toKootaValueTrait(valueTrait), valueToSet);
  }

  function TargetFor<TTrait extends ITrait>(
    relation: IRelation<TTrait>,
    entity: EntityId
  ): Option<EntityId> {
    return (entity as Entity).targetFor(toKootaRelation(relation));
  }

  function TargetsFor<TTrait extends ITrait>(
    relation: IRelation<TTrait>,
    entity: EntityId
  ): Iterable<EntityId> {
    return (entity as Entity).targetsFor(toKootaRelation(relation));
  }

  return {
    Add,
    Destroy,
    Get,
    Has,
    Remove,
    Set,
    TargetFor,
    TargetsFor,
  };
}

export function createTraitFactory(): ITraitFactory {
  function fromKootaTrait<TKootaTrait extends Trait<any>>(
    trait: TKootaTrait,
    isTag: boolean
  ): ITrait {
    return getOrCreateWrapper(trait, (t) => ({ IsTag: isTag, trait: t }));
  }

  function fromKootaTagTrait(trait: TagTrait): ITagTrait {
    return fromKootaTrait(trait, true);
  }

  function fromKootaValueTrait<T>(trait: KootaValueTrait<T>): IValueTrait<T> {
    return fromKootaTrait(trait, false);
  }

  function fromKootaRelation<TKootaTrait extends Trait<any>, TTrait extends ITrait>(
    rel: Relation<TKootaTrait>,
    isTag: boolean
  ): IRelation<TTrait> {
    function On(entity: EntityId): TTrait {
      return fromKootaTrait(rel(entity as Entity), isTag) as TTrait;
    }

    function Wildcard(): TTrait {
      return fromKootaTrait(rel("*"), isTag) as TTrait;
    }

    return getOrCreateWrapper(rel, (r) => ({ IsTag: isTag, rel: r, On, Wildcard }));
  }

  function fromKootaTagRelation(rel: Relation<TagTrait>): IRelation<ITagTrait> {
    return fromKootaRelation(rel, true);
  }

  function fromKootaValueRelation<T>(rel: Relation<KootaValueTrait<T>>): IRelation<IValueTrait<T>> {
    return fromKootaRelation(rel, false);
  }

  function Relation(exclusive: boolean): IRelation<ITagTrait> {
    const rel: Relation<TagTrait> = exclusive ? relation({ exclusive: true }) : relation();
    return fromKootaTagRelation(rel);
  }

  function RelationWith<T>(exclusive: boolean, store: T): IRelation<IValueTrait<T>> {
    const typedStore = store as KootaSchema<T>;
    const rel = exclusive
      ? relation({ exclusive: true, store: typedStore })
      : relation({ store: typedStore });
    return fromKootaValueRelation(rel);
  }

  function TagTrait(): ITagTrait {
    return fromKootaTagTrait(trait());
  }

  function TraitWith<T>(value: T): IValueTrait<T> {
    const traitDef = trait(value as KootaSchema<T>) as KootaValueTrait<T>;
    return fromKootaValueTrait(traitDef);
  }

  function TraitWithRef<T>(valueFactory: () => T): IValueTrait<T> {
    const f = valueFactory as T extends Schema ? AoSFactory : never;
    const traitDef = trait(f) as KootaValueTrait<T>;
    return fromKootaValueTrait(traitDef);
  }

  return {
    Relation,
    RelationWith,
    TagTrait,
    TraitWith,
    TraitWithRef,
  };
}

export function fromKootaWorld(world: World): IWorld {
  type WrappedWorld = IWorld & { world: World };

  function newWrapper(): WrappedWorld {
    function unwrapQueryOperators(ops: QueryOperator[]) {
      return ops.map((op) =>
        QueryOperator_Visit(
          toKootaTrait,
          (ts) => Not(...ts),
          (ts) => Or(...ts),
          op
        )
      );
    }

    function wrapQueryResult<S, T>(result: QueryResult<KootaQueryParameters<S>>): IQueryResult<T> {
      function UpdateEach(callback: (state: [T, EntityId]) => void): void {
        result.updateEach(
          (state: InstancesFromParameters<KootaQueryParameters<S>>, entity: Entity) => {
            // The type assertion below should be ok. I worked pretty hard to make sure the types line up here...
            callback([state as T, entity]);
          }
        );
      }

      return { Select: () => wrapQueryResult<void, void>(result.select()), UpdateEach };
    }

    return new (class implements IWorld {
      readonly world: World = world;

      Get<T>(valueTrait: IValueTrait<T>): Option<T> {
        return this.world.get(toKootaValueTrait(valueTrait));
      }

      Has(someTrait: ITrait): boolean {
        return this.world.has(toKootaTrait(someTrait));
      }

      Query(...where: QueryOperator[]): IQueryResult<void> {
        const queryParameters = unwrapQueryOperators(where);
        const result = this.world.query(...queryParameters);
        return wrapQueryResult(result);
      }

      QueryFirst(...where: QueryOperator[]): Option<EntityId> {
        const queryParameters = unwrapQueryOperators(where);
        return this.world.queryFirst(...queryParameters);
      }

      QueryTrait<T>(someTrait: IValueTrait<T>, ...where: QueryOperator[]): IQueryResult<T> {
        const queryParameters = unwrapQueryOperators(where);
        const result = this.world.query(toKootaValueTrait(someTrait), ...queryParameters);
        return wrapQueryResult(result);
      }

      QueryTraits<T, U>(
        traits: [IValueTrait<T>, IValueTrait<U>],
        ...where: QueryOperator[]
      ): IQueryResult<[T, U]> {
        const queryParameters = unwrapQueryOperators(where);
        const [first, second] = traits;
        const result = this.world.query(
          toKootaValueTrait(first),
          toKootaValueTrait(second),
          ...queryParameters
        );
        return wrapQueryResult(result);
      }

      Remove(someTrait: ITrait): void {
        return this.world.remove(toKootaTrait(someTrait));
      }

      Spawn(...traits: TraitSpec[]): EntityId {
        function unwrapTraitSpec(c: TraitSpec): ConfigurableTrait<Trait<any>> {
          return TraitSpec_Map(
            toKootaTrait,
            ([traitWrapper, value]) =>
              [toKootaTrait(traitWrapper), value] as ConfigurableTrait<Trait<any>>,
            c
          );
        }

        return this.world.spawn(...traits.map(unwrapTraitSpec));
      }
    })();
  }

  return getOrCreateWrapper(world, () => newWrapper());
}
