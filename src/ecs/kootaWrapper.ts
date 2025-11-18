import {
  EntityId,
  IEntityOperations,
  IQueryResult$2 as IQueryResult,
  IRelation$1 as IRelation,
  ITagTrait,
  ITrait,
  ITraitFactory,
  IValueTrait$1 as IValueTrait,
  IMutableValueTrait$2 as IMutableValueTrait,
  IWorld,
  QueryOperator,
  RelationConfig,
  TraitSpec_$union as TraitSpec,
  TraitSpec_Map,
} from "../generated/ECS/Types";
import { Option } from "../generated/fable_modules/fable-library-ts.4.27.0/Option";
import {
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
  SetTraitCallback,
  TagTrait,
  trait,
  Trait,
  TraitValue,
  World,
} from "koota";

type KootaSchema<T> = T extends Schema ? T : never;
type KootaValueFactory<T> = () => T extends number
  ? number
  : T extends boolean
    ? boolean
    : T extends string
      ? string
      : T;
type KootaValueTrait<T> = Trait<KootaSchema<T>>;
type KootaValueFactoryTrait<T> = Trait<KootaValueFactory<T>>;
type KootaQueryParameters<S> = S extends QueryParameter[] ? S : [];

type WrappedTrait<TKootaTrait extends Trait<any>> = { IsTag: boolean; trait: TKootaTrait };
type WrappedTagTrait = WrappedTrait<TagTrait>;
type WrappedValueTrait<T> = WrappedTrait<KootaValueTrait<T>>;
type WrappedValueFactoryTrait<T> = WrappedTrait<KootaValueFactoryTrait<T>>;

type WrappedRelation<TKootaTrait extends Trait<any>, TTrait extends ITrait> = IRelation<TTrait> & {
  IsTag: boolean;
  rel: Relation<TKootaTrait>;
};

type WrappedTagRelation = WrappedRelation<TagTrait, ITagTrait>;

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
  const objWithWrapper =
    $wrapper in obj
      ? (obj as WithWrapper<T, TWrapper>)
      : Object.assign(obj, {
          [$wrapper]: createWrapper(obj),
        });

  return objWithWrapper[$wrapper];
}

function validateWrappedTrait<TKootaTrait extends Trait<any>, T extends WrappedTrait<TKootaTrait>>(
  trait: ITrait,
  method: string
): T {
  if (!("trait" in trait)) {
    throw new Error(`Invalid ITrait implementation passed to ${method}().`);
  }

  return trait as T;
}

function toKootaTrait<T>(trait: ITrait): Trait<any> {
  const method = "toKootaTrait";
  const traitWrapper = trait.IsTag
    ? validateWrappedTrait<TagTrait, WrappedTagTrait>(trait, method)
    : validateWrappedTrait<KootaValueTrait<T>, WrappedValueTrait<T>>(trait, method);
  return traitWrapper.trait;
}

function toKootaValueTraitForRead<T>(trait: IValueTrait<T>): KootaValueTrait<T> {
  return validateWrappedTrait<KootaValueTrait<T>, WrappedValueTrait<T>>(
    trait,
    "toKootaValueTraitForRead"
  ).trait;
}

// We only export the strongly-typed versions, otherwise type inference fails to find the most
// specific trait type and things break.
export function toKootaValueTrait<T, TMutable>(
  trait: IMutableValueTrait<T, TMutable>
): KootaValueTrait<T> {
  return validateWrappedTrait<KootaValueTrait<T>, WrappedValueTrait<T>>(trait, "toKootaValueTrait")
    .trait;
}

export function toKootaValueFactoryTrait<T, TMutable>(
  trait: IMutableValueTrait<T, TMutable>
): KootaValueFactoryTrait<T> {
  return validateWrappedTrait<KootaValueFactoryTrait<T>, WrappedValueFactoryTrait<T>>(
    trait,
    "toKootaValueFactoryTrait"
  ).trait;
}

export function toKootaTagTrait(trait: ITagTrait): TagTrait {
  return validateWrappedTrait<TagTrait, WrappedTagTrait>(trait, "toKootaTagTrait").trait;
}

export function toKootaRelation<TTrait extends ITrait, T = void>(
  r: IRelation<TTrait>
): Relation<Trait<any>> {
  if (!("rel" in r)) {
    throw new Error("Invalid IRelation implementation passed to toKootaRelation().");
  }
  const wrapper = r.IsTag
    ? (r as WrappedTagRelation)
    : (r as WrappedRelation<KootaValueTrait<T>, TTrait>);
  return wrapper.rel;
}

export function createEntityOperations(): IEntityOperations {
  function Add(someTrait: ITrait, entity: EntityId & Entity): void {
    entity.add(toKootaTrait(someTrait));
  }

  function Destroy(entity: EntityId & Entity): void {
    entity.destroy();
  }

  function Get<T>(valueTrait: IValueTrait<T>, entity: EntityId & Entity): Option<T> {
    return entity.get(toKootaValueTraitForRead(valueTrait));
  }

  function Has(someTrait: ITrait, entity: EntityId & Entity): boolean {
    return entity.has(toKootaTrait(someTrait));
  }

  function Remove(someTrait: ITrait, entity: EntityId & Entity): void {
    entity.remove(toKootaTrait(someTrait));
  }

  function Set<T>(valueTrait: IValueTrait<T>, value: T, entity: EntityId & Entity): void {
    const valueToSet = value as TraitValue<KootaSchema<T>>;
    entity.set(toKootaValueTraitForRead(valueTrait), valueToSet);
  }

  function SetWith<T>(
    valueTrait: IValueTrait<T>,
    update: (value: T) => T,
    entity: EntityId & Entity
  ): void {
    entity.set(
      toKootaValueTraitForRead(valueTrait),
      update as SetTraitCallback<KootaValueTrait<T>>
    );
  }

  function TargetFor<TTrait extends ITrait>(
    relation: IRelation<TTrait>,
    entity: EntityId & Entity
  ): Option<EntityId> {
    return entity.targetFor(toKootaRelation(relation));
  }

  function TargetsFor<TTrait extends ITrait>(
    relation: IRelation<TTrait>,
    entity: EntityId & Entity
  ): Iterable<EntityId> {
    return entity.targetsFor(toKootaRelation(relation));
  }

  return {
    Add,
    Destroy,
    Get,
    Has,
    Remove,
    Set,
    SetWith,
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

  function fromKootaValueTrait<T, TMutable>(
    trait: KootaValueTrait<T>
  ): IMutableValueTrait<T, TMutable> {
    return fromKootaTrait(trait, false);
  }

  function fromKootaValueFactoryTrait<T>(
    trait: KootaValueFactoryTrait<T>
  ): IMutableValueTrait<T, T> {
    return fromKootaTrait(trait, false);
  }

  function fromKootaRelation<TKootaTrait extends Trait<any>, TTrait extends ITrait>(
    rel: Relation<TKootaTrait>,
    isTag: boolean
  ): IRelation<TTrait> {
    function On(entity: EntityId & Entity): TTrait {
      return fromKootaTrait(rel(entity), isTag) as TTrait;
    }

    function Wildcard(): TTrait {
      return fromKootaTrait(rel("*"), isTag) as TTrait;
    }

    return getOrCreateWrapper(rel, (r) => ({ IsTag: isTag, rel: r, On, Wildcard }));
  }

  function fromKootaTagRelation(rel: Relation<TagTrait>): IRelation<ITagTrait> {
    return fromKootaRelation(rel, true);
  }

  function fromKootaValueRelation<T, TMutable>(
    rel: Relation<KootaValueTrait<T>>
  ): IRelation<IMutableValueTrait<T, TMutable>> {
    return fromKootaRelation(rel, false);
  }

  function Relation(config: RelationConfig): IRelation<ITagTrait> {
    const rel: Relation<TagTrait> = config.IsExclusive ? relation({ exclusive: true }) : relation();
    return fromKootaTagRelation(rel);
  }

  // We ignore the mutableStore parameter; It's only there for type inference on the F# side.
  function RelationWith<T, TMutable>(
    config: RelationConfig,
    store: T
  ): IRelation<IMutableValueTrait<T, TMutable>> {
    const typedStore = store as KootaSchema<T>;
    const rel = config.IsExclusive
      ? relation({ exclusive: true, store: typedStore })
      : relation({ store: typedStore });
    return fromKootaValueRelation(rel);
  }

  function TagTrait(): ITagTrait {
    return fromKootaTagTrait(trait());
  }

  // We ignore the mutableValue parameter; It's only there for type inference on the F# side.
  function TraitWith<T, TMutable>(value: T): IMutableValueTrait<T, TMutable> {
    const traitDef = trait(value as KootaSchema<T>) as KootaValueTrait<T>;
    return fromKootaValueTrait(traitDef);
  }

  function TraitWithRef<T>(valueFactory: () => T): IMutableValueTrait<T, T> {
    return fromKootaValueFactoryTrait(trait(valueFactory));
  }

  return {
    Relation,
    RelationWith,
    TagTrait,
    TraitWith,
    TraitWithRef,
  };
}

type WrappedWorld = IWorld & { world: World };

export function fromKootaWorld(world: World): IWorld {
  function newWrapper(): WrappedWorld {
    function unwrapQueryOperators(ops: QueryOperator[]): QueryParameter[] {
      return ops.map((op) => {
        switch (op.type) {
          case "with":
            return toKootaTrait(op.Item);
          case "not":
            const notOperands = op.Item.map(toKootaTrait);
            return Not(...notOperands);
          case "or":
            const orOperands = op.Item.map(toKootaTrait);
            return Or(...orOperands);
        }
      });
    }

    // NOTE:
    // For QueryResult, on the F# side, there are three cases for the T/TMutable type parameter that
    // don't quite map to TypeScript (we'll ignore the mutable type from here on for brevity):
    // 1. unit: Has a unit * EntityId callback, which maps to [undefined, EntityId] in TypeScript.
    // 2. T: Has a T * EntityId callback, which maps to [T, EntityId] in TypeScript.
    // 3. T * U (and future generalizations of higher arity): Was a (T * U) * EntityId callback,
    //    which maps to [[T, U], EntityId] in TypeScript.
    // In the functions below, we map state/trait values accordingly based on the given arity.
    function wrapVoidQueryResult<S>(
      result: QueryResult<KootaQueryParameters<S>>
    ): IQueryResult<void, void> {
      function ForEach(callback: (state: [void, EntityId]) => void): void {
        for (const entity of result) {
          callback([undefined, entity]);
        }
      }

      function UpdateEach(callback: (state: [void, EntityId]) => void): void {
        function thunk(state: InstancesFromParameters<KootaQueryParameters<S>>, entity: Entity) {
          callback([undefined, entity]);
        }
        result.updateEach(thunk);
      }

      return {
        ForEach,
        UpdateEach,
        [Symbol.iterator](): Iterator<EntityId> {
          return result[Symbol.iterator]();
        },
      };
    }

    function wrapQueryResult1<S, T, TMutable>(
      result: QueryResult<KootaQueryParameters<S>>,
      valueTrait: KootaValueTrait<T>
    ): IQueryResult<T, TMutable> {
      function ForEach(callback: (state: [T, EntityId]) => void): void {
        for (const entity of result) {
          const value = entity.get(valueTrait)!; // Trait must exist per the query that created this result.
          callback([value, entity]);
        }
      }

      function UpdateEach(callback: (state: [TMutable, EntityId]) => void): void {
        function thunk(state: InstancesFromParameters<KootaQueryParameters<S>>, entity: Entity) {
          callback([state[0], entity]);
        }
        result.updateEach(thunk);
      }

      return {
        ForEach,
        UpdateEach,
        [Symbol.iterator](): Iterator<EntityId> {
          return result[Symbol.iterator]();
        },
      };
    }

    function wrapQueryResult2<S, T, TMutable, U, UMutable>(
      result: QueryResult<KootaQueryParameters<S>>,
      valueTraits: [KootaValueTrait<T>, KootaValueTrait<U>]
    ): IQueryResult<[T, U], [TMutable, UMutable]> {
      function ForEach(callback: (state: [[T, U], EntityId]) => void): void {
        for (const entity of result) {
          const value1 = entity.get(valueTraits[0])!; // Traits must exist per the query that created this result.
          const value2 = entity.get(valueTraits[1])!;
          callback([[value1, value2], entity]);
        }
      }

      function UpdateEach(callback: (state: [[TMutable, UMutable], EntityId]) => void): void {
        function thunk(state: InstancesFromParameters<KootaQueryParameters<S>>, entity: Entity) {
          callback([state.slice(0, 2) as [TMutable, UMutable], entity]);
        }
        result.updateEach(thunk);
      }

      return {
        ForEach,
        UpdateEach,
        [Symbol.iterator](): Iterator<EntityId> {
          return result[Symbol.iterator]();
        },
      };
    }

    return new (class implements IWorld {
      readonly world: World = world;

      Add(someTrait: ITrait): void {
        this.world.add(toKootaTrait(someTrait));
      }

      Get<T>(valueTrait: IValueTrait<T>): Option<T> {
        return this.world.get(toKootaValueTraitForRead(valueTrait));
      }

      Has(someTrait: ITrait): boolean {
        return this.world.has(toKootaTrait(someTrait));
      }

      Query(...where: QueryOperator[]): IQueryResult<void, void> {
        const queryParameters = unwrapQueryOperators(where);
        const result = this.world.query(...queryParameters);
        return wrapVoidQueryResult(result);
      }

      QueryFirst(...where: QueryOperator[]): Option<EntityId> {
        const queryParameters = unwrapQueryOperators(where);
        return this.world.queryFirst(...queryParameters);
      }

      QueryTrait<T, TMutable>(
        someTrait: IMutableValueTrait<T, TMutable>,
        ...where: QueryOperator[]
      ): IQueryResult<T, TMutable> {
        const queryParameters = unwrapQueryOperators(where);
        const kootaValueTrait = toKootaValueTrait(someTrait);
        const result = this.world.query(kootaValueTrait, ...queryParameters);
        return wrapQueryResult1(result, kootaValueTrait);
      }

      QueryTraits<T, TMutable, U, UMutable>(
        firstTrait: IMutableValueTrait<T, TMutable>,
        secondTrait: IMutableValueTrait<U, UMutable>,
        ...where: QueryOperator[]
      ): IQueryResult<[T, U], [TMutable, UMutable]> {
        const queryParameters = unwrapQueryOperators(where);
        const firstKootaValueTrait = toKootaValueTrait(firstTrait);
        const secondKootaValueTrait = toKootaValueTrait(secondTrait);
        const result = this.world.query(
          firstKootaValueTrait,
          secondKootaValueTrait,
          ...queryParameters
        );
        return wrapQueryResult2(result, [firstKootaValueTrait, secondKootaValueTrait]);
      }

      Remove(someTrait: ITrait): void {
        return this.world.remove(toKootaTrait(someTrait));
      }

      Set<T>(valueTrait: IValueTrait<T>, value: T): void {
        const valueToSet = value as TraitValue<KootaSchema<T>>;
        world.set(toKootaValueTraitForRead(valueTrait), valueToSet);
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

export function toKootaWorld(world: IWorld): World {
  if (!("world" in world)) {
    throw new Error("Invalid IWorld implementation passed to toKootaWorld.");
  }
  const wrapper = world as WrappedWorld;
  return wrapper.world;
}

export type { IWorld };
