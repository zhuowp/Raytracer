﻿using System.Drawing;

using Raytracer.Math;
using Raytracer.Lights;
using Raytracer.Materials;
using Raytracer.Rendering;
using Raytracer.Rendering.Intersection;

namespace Raytracer.Objects
{
    public abstract class AbstractObject3d
    {
        public string Name { get; set; }
        public Material Material { get; set; }

        public abstract Vector3d GetNormal(IIntersectionResult intersectionResult);

        public abstract Vector2d GetUVCoordinates(IIntersectionResult intersectionResult);

        public abstract IIntersectionResult Intersection(Vector3d direction, Vector3d position);

        public virtual Color GetColor(Vector3d direction, IIntersectionResult intersectionResult, Scene scene)
        {
            Color3f result = new Color3f();

            Color3f surfaceColor = Material.Texture != null ? Material.Texture.GetColor(GetUVCoordinates(intersectionResult), 0) : Material.Color;
            Color3f reflectionColor = new Color3f();

            if (Material.Reflection > 0.0f)
            {
                // Reflection
                Vector3d reflectionVector = VectorHelpers.GetReflectionVector(direction, GetNormal(intersectionResult));

                IIntersectionResult reflectionIntersectionResult = scene.GetNearestObjectIntersection(reflectionVector, intersectionResult.Intersection, this);

                if (reflectionIntersectionResult != null)
                {
                    reflectionColor = new Color3f(intersectionResult.Object.GetColor(direction, reflectionIntersectionResult, scene));
                }
            }

            foreach (AbstractLight light in scene.Lights)
            {
                bool lightVisible = true;

                if (light.Position != null)
                {
                    Vector3d lightDistanceVector = light.Position - intersectionResult.Intersection;
                    double lightDistance = lightDistanceVector.Length();

                    foreach (AbstractObject3d o in scene.Objects)
                    {
                        if (o == this)
                        {
                            continue;
                        }

                        IIntersectionResult lightIntersectionResult = o.Intersection(Vector3d.Normalize(lightDistanceVector), intersectionResult.Intersection);

                        if (lightIntersectionResult != null)
                        {
                            if (lightIntersectionResult.IntersectionDistance < lightDistance)
                            {
                                lightVisible = false;
                                break;
                            }
                        }
                    }
                }

                if (!lightVisible)
                {
                    continue;
                }

                Vector3d normal = GetNormal(intersectionResult);
                Vector3d lightDirection = -light.GetDirection(intersectionResult.Intersection);

                double distanceSquared = light.GetDistance(intersectionResult.Intersection);
                distanceSquared *= distanceSquared;

                double normalDotLightDirection = normal.Dot(lightDirection);
                double diffuseIntensity = ScalarHelpers.Saturate(normalDotLightDirection);

                Color3f diffuse = diffuseIntensity * light.DiffuseColor * light.DiffusePower / distanceSquared;

                Vector3d halfwayVector = (lightDirection - direction).Normalize();

                double normalDotHalfwayVector = normal.Dot(halfwayVector);
                double specularIntensity = System.Math.Pow(ScalarHelpers.Saturate(normalDotHalfwayVector), 16f);

                Color3f specular = specularIntensity * light.SpecularColor * light.SpecularPower / distanceSquared;

                //return (surfaceColor * (diffuse + specular)).ToColor();
                result += diffuse + specular;
            }

            return (((surfaceColor * result) * (1.0 - Material.Reflection)) + (reflectionColor * Material.Reflection)).ToColor();
        }
    }
}