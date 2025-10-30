import { World } from "koota";
import { MathUtils } from "three";
import { Position, TargetPosition } from "./traits";

export function animate({ world, delta }: { world: World; delta: number }) {
  world.query(Position, TargetPosition).updateEach(([pos, targetPos], entity) => {
    const lambda = 6;
    const [tx, ty, tz] = [targetPos.x, targetPos.y, targetPos.z];
    const [x, y, z] = [
      MathUtils.damp(pos.x, tx, lambda, delta),
      MathUtils.damp(pos.y, ty, lambda, delta),
      MathUtils.damp(pos.z, tz, lambda, delta),
    ];

    // We need some tolerance due to funny business with IEEE 754 equality.
    const closeEnough = 0.01;
    if (
      Math.abs(x - tx) < closeEnough &&
      Math.abs(y - ty) < closeEnough &&
      Math.abs(z - tz) < closeEnough
    ) {
      // Animation is finished; Set exactly to target and remove TargetPosition.
      pos.x = tx;
      pos.y = ty;
      pos.z = tz;
      entity.remove(TargetPosition);
    } else {
      pos.x = x;
      pos.y = y;
      pos.z = z;
    }
  });
}
