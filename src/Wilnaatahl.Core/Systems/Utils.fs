module Wilnaatahl.Systems.Utils

open System

/// Smoothly interpolates from current to target using exponential damping.
/// current: current value
/// target: target value
/// lambda: smoothing factor (>0, higher = faster)
/// delta: time step in seconds (>0)
let inline damp (current: float) (target: float) (lambda: float) (delta: float) : float =
    // This can also be implemented in terms of lerp, but this is faster.
    current + (target - current) * (1.0 - Math.Exp(-lambda * delta))
