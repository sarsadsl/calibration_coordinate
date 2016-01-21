static class NewTransfer
        {
            public static Vector3D origin = new Vector3D();
            public static Vector3D xais = new Vector3D();
            public static Vector3D yais = new Vector3D();
            public static Vector3D zais = new Vector3D();

            public static Matrix3D Pmatrix;
            public static Matrix3D Fmatrix;
            public static Matrix3D Wmatrix;
            // |x A B D| -1      |30*x.x 30*y.x 30*z.x o.x+x.x+y.x+z.x|
            // |A y C E|         |30*x.y 30*y.y 30*z.y o.y+x.y+y.y+z.y|
            // |B C z F|    *    |30*x.z 30*y.z 30*z.z o.z+x.z+y.z+z.z|
            // |D E F 4|         |30     30     30     4              |
            public static void CalcP()
            {
                double x, A, B, C, y, D, E, F, z;
                x = xais.X * xais.X + yais.X * yais.X + zais.X * zais.X + origin.X * origin.X;

                y = xais.Y * xais.Y + yais.Y * yais.Y + zais.Y * zais.Y + origin.Y * origin.Y;

                z = xais.Z * xais.Z + yais.Z * yais.Z + zais.Z * zais.Z + origin.Z * origin.Z;

                A = origin.X * origin.Y + xais.X * xais.Y + yais.X * yais.Y + zais.X * zais.Y;

                B = origin.X * origin.Z + xais.X * xais.Z + yais.X * yais.Z + zais.X * zais.Z;

                C = origin.Y * origin.Z + xais.Y * xais.Z + yais.Y * yais.Z + zais.Y * zais.Z;

                D = origin.X + xais.X + yais.X + zais.X;
                E = yais.Y + xais.Y + zais.Y + origin.Y;
                F = yais.Z + xais.Z + zais.Z + origin.Z;

                NewTransfer.Pmatrix = new Matrix3D(x, A, B, D,
                                                   A, y, C, E,
                                                   B, C, z, F,
                                                   D, E, F, 4);
                NewTransfer.Pmatrix.Invert();

            }
            public static void CalcF()
            {
                double A, B, C;
                A = origin.X + xais.X + yais.X + zais.X;
                B = yais.Y + xais.Y + zais.Y + origin.Y;
                C = yais.Z + xais.Z + zais.Z + origin.Z;
                NewTransfer.Fmatrix = new Matrix3D(30 * xais.X, 30 * yais.X, 30 * zais.X, A,
                                                   30 * xais.Y, 30 * yais.Y, 30 * zais.Y, B,
                                                   30 * xais.Z, 30 * yais.Z, 30 * zais.Z, C,
                                                   30, 30, 30, 4);
            }
            public static void CalcW()
            {
                NewTransfer.Wmatrix = Matrix3D.Multiply(Pmatrix, Fmatrix);
            }
            public static Vector3D CalcNp(Vector3D point)
            {
                Vector3D np = new Vector3D();
                np.X = NewTransfer.Wmatrix.M11 * point.X + NewTransfer.Wmatrix.M21 * point.Z + NewTransfer.Wmatrix.M31 * point.Y + NewTransfer.Wmatrix.OffsetX * 1;
                np.Y = NewTransfer.Wmatrix.M12 * point.X + NewTransfer.Wmatrix.M22 * point.Z + NewTransfer.Wmatrix.M32 * point.Y + NewTransfer.Wmatrix.OffsetY * 1;
                np.Z = NewTransfer.Wmatrix.M13 * point.X + NewTransfer.Wmatrix.M23 * point.Z + NewTransfer.Wmatrix.M33 * point.Y + NewTransfer.Wmatrix.OffsetZ * 1;
                return np;
            }            
        }
        private static void Set()
        {
            NewTransfer.origin = O;
            NewTransfer.xais = X;
            NewTransfer.yais = Y;
            NewTransfer.zais = Z;
            NewTransfer.CalcP();
            NewTransfer.CalcF();
            NewTransfer.CalcW();

        }