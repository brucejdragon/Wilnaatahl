namespace Wilnaatahl.ViewModel

type Vector3<[<Measure>] 'u> = {
    X: float<'u>
    Y: float<'u>
    Z: float<'u>
} with

    /// Adds two vectors component-wise.
    static member inline (+)(v1, v2) = { X = v1.X + v2.X; Y = v1.Y + v2.Y; Z = v1.Z + v2.Z }

    /// Subtracts the second vector from the first component-wise.
    static member inline (-)(v1, v2) = { X = v1.X - v2.X; Y = v1.Y - v2.Y; Z = v1.Z - v2.Z }

    /// Calculates the cross product of two vectors.
    static member inline (*)(v1, v2) = {
        X = v1.Y * v2.Z - v1.Z * v2.Y
        Y = v1.Z * v2.X - v1.X * v2.Z
        Z = v1.X * v2.Y - v1.Y * v2.X
    }

    /// Multiplies a vector by a scalar.
    static member inline (*)(v, k) = { X = v.X * k; Y = v.Y * k; Z = v.Z * k }

    /// Multiplies a vector by a scalar.
    static member inline (*)(k: float, v: Vector3<'u>) = v * k

    /// Computes the dot product of two vectors.
    static member inline (.*)(v1, v2) = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z

    /// Divides a vector by a scalar.
    static member inline (/)(v, k) = { X = v.X / k; Y = v.Y / k; Z = v.Z / k }

module Vector =

    /// Creates a Vector3 from an anonymous record with x, y, z fields.
    let fromPosition (pos: {| x: float; y: float; z: float |}) = { X = pos.x; Y = pos.y; Z = pos.z }

    /// Creates a Vector3 from a tuple of component values.
    let fromComponents (x, y, z) = { X = x; Y = y; Z = z }

    /// Small threshold to avoid numerical instability when normalizing vectors.
    let nearZero = 1e-9

    /// Returns the Euclidean length (magnitude) of the vector.
    let inline length v =
        sqrt (v.X * v.X + v.Y * v.Y + v.Z * v.Z)

    /// Returns the normalized (unit length) vector, or the original if zero length.
    let inline normalize v =
        let len = length v
        if len <= nearZero then v else v / len

    /// Returns a vector containing components that are the higher of the two vectors'.
    let inline max v1 v2 = { X = max v1.X v2.X; Y = max v1.Y v2.Y; Z = max v1.Z v2.Z }

    /// Returns a vector containing components that are the higher of the two vectors'.
    let inline min v1 v2 = { X = min v1.X v2.X; Y = min v1.Y v2.Y; Z = min v1.Z v2.Z }

    /// Linearly interpolates between two vectors by alpha (0.0 - 1.0).
    let inline lerp (v1: Vector3<'u>) (v2: Vector3<'u>) alpha = v1 + (v2 - v1) * alpha

    /// Smoothly damps v1 toward v2 using exponential smoothing.
    let damp v1 v2 lambda delta =
        lerp v1 v2 (1.0 - exp (-lambda * delta))
