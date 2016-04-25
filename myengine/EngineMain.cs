﻿using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using MyEngine.Components;

namespace MyEngine
{


    public class EngineMain : GameWindow
    {


        public InputSystem Input { get; private set; }
        public AssetSystem Asset { get; private set; }
        List<SceneSystem> scenes = new List<SceneSystem>();

        public EngineMain() : base(
            1400,
            900,
            new GraphicsMode(),
            "MyEngine",
            GameWindowFlags.Default,
            DisplayDevice.Default,
            4,
            0,
            GraphicsContextFlags.ForwardCompatible
        )
        {

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;

            VSync = VSyncMode.Off;
            TargetRenderFrequency = 0;

            //Texture2D.InitTexture2D();
            UnloadFactory.Set(ref ubo, new UniformBlock());
            //new PhysicsUsage.PhysicsManager();
            Asset = new AssetSystem();
            Input = new InputSystem(this);

            stopwatchSinceStart.Restart();

            StartSec();


            //{
            //    var winForm = new Panels.DebugValuesTable();
            //    winForm.Show();
            //}

        }


        System.Diagnostics.Stopwatch stopwatchSinceStart = new System.Diagnostics.Stopwatch();

        public Cubemap skyboxCubeMap;


        internal UnloadFactory unloadFactory = new UnloadFactory();
        internal static UniformBlock ubo;
        Mesh quadMesh;
        Mesh skyboxMesh;
        Shader finalDrawShader;

        public SceneSystem AddScene()
        {
            var s = new SceneSystem(this);
            AddScene(s);
            return s;
        }


        protected override void OnLoad(System.EventArgs e)
        {

            quadMesh = Factory.GetMesh("internal/quad.obj");
            skyboxMesh = Factory.GetMesh("internal/skybox.obj");
            finalDrawShader = Factory.GetShader("internal/finalDraw.glsl");

            foreach (StringName r in System.Enum.GetValues(typeof(StringName)))
            {
                if (r == StringName.Extensions) break;
                Debug.Info(r.ToString() + ": " + GL.GetString(r));
            }
            //Debug.Info(StringName.Extensions.ToString() + ": " + GL.GetString(StringName.Extensions));


            this.VSync = VSyncMode.Off;

            // Other state
            GL.Enable(EnableCap.Texture2D);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Enable(EnableCap.Multisample);


            //GL.ClearColor(System.Drawing.Color.MidnightBlue);
            GL.ClearColor(System.Drawing.Color.Black);

        }



        protected override void OnUnload(EventArgs e)
        {
            //PhysicsUsage.PhysicsManager.instance.CleanUp();
            foreach (var i in UnloadFactory.unloadables)
            {
                i.Unload();
            }
        }




        DeferredGBuffer gBuffer;


        protected override void OnResize(EventArgs e)
        {
            foreach (var scene in scenes)
            {
                scene.mainCamera.SetSize(ClientSize.Width, ClientSize.Height);
            }

            UnloadFactory.Set(ref gBuffer, new DeferredGBuffer(Width, Height));

            //screenCenter = new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
            //windowCenter = new Point(Width / 2, Height / 2);

            Debug.Info("Windows resized to: width:" + ClientSize.Width + " height:" + ClientSize.Height);
        }

        public void AddScene(SceneSystem scene)
        {
            scenes.Add(scene);
        }

        bool drawShadowMapContents = false;
        bool drawGBufferContents = false;
        bool drawLines = false;
        bool enablePostProcessEffects = false;
        bool debugBounds = true;
        bool shadowsEnabled = true;

        public override void Exit()
        {
            if (IsDisposed) return;
            if (IsExiting) return;
            base.Exit();
        }

        void StartSec()
        {
            eventThreadTime.Restart();
            /*
            var t = new Thread(() =>
            {
                while (this.IsDisposed == false && this.IsExiting == false)
                {
                    EventThreadMain();
                }
            });

            t.Priority = ThreadPriority.Highest;
            t.IsBackground = true;
            t.Start();
            */
        }

        System.Diagnostics.Stopwatch eventThreadTime = new System.Diagnostics.Stopwatch();
        ManualResetEvent onRenderGameWaitHandle = new ManualResetEvent(false);

        void EventThreadMain()
        {

            //Debug.Tick("eventThread");
            var deltaTime = eventThreadTime.ElapsedMilliseconds / 1000.0;
            eventThreadTime.Restart();

            this.Title = string.Join("\t  ", Debug.stringValues.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Key + ":" + kvp.Value).ToArray());

            if (this.Focused) Input.Update();

            var scene = scenes[0];
            scene.EventSystem.Raise(new MyEngine.Events.GraphicsUpdate(deltaTime));

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //Debug.AddValue("fps", (1 / e.Time).ToString());
            Debug.Tick("renderThread");

            EventThreadMain();

            ubo.engine.totalElapsedSecondsSinceEngineStart = (float)stopwatchSinceStart.Elapsed.TotalSeconds;
            ubo.engine.gammaCorrectionTextureRead = 2.2f;
            ubo.engine.gammaCorrectionFinalColor = 1/2.2f;

            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var scene = scenes[0];

            var camera = scene.mainCamera;
            var allEntitys = scene.Entities;


            var countMeshesRendered = 0;


            if (Input.GetKeyDown(OpenTK.Input.Key.F11)) enablePostProcessEffects = !enablePostProcessEffects;
            if (Input.GetKeyDown(OpenTK.Input.Key.F10)) drawLines = !drawLines;
            if (Input.GetKeyDown(OpenTK.Input.Key.F9)) drawGBufferContents = !drawGBufferContents;
            if (Input.GetKeyDown(OpenTK.Input.Key.F8)) drawShadowMapContents = !drawShadowMapContents;
            if (Input.GetKeyDown(OpenTK.Input.Key.F7)) debugBounds = !debugBounds;
            if (Input.GetKeyDown(OpenTK.Input.Key.F6)) shadowsEnabled = !shadowsEnabled;
            if (Input.GetKeyDown(OpenTK.Input.Key.F5)) Factory.ReloadAllShaders();


            var frustrumPlanes = camera.GetFrustumPlanes();

            var allRenderers = scene.Renderers;
            {
                camera.UploadDataToUBO(ubo); // bind camera view params
                                             //GL.BeginQuery(QueryTarget.)

                // G BUFFER GRAB PASS
                {
                    gBuffer.BindAllFrameBuffersForDrawing();


                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(true);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

                    // SKYBOX PASS
                    if(skyboxCubeMap != null)
                    {
                        GL.DepthRange(0.999, 1);
                        GL.DepthMask(false);

                        var shader = Factory.GetShader("internal/deferred.skybox.shader");
                        shader.Uniforms.Set("skyboxCubeMap", skyboxCubeMap);
                        shader.Bind();

                        skyboxMesh.Draw();
                        GL.DepthRange(0, 1);
                    }



                    if (drawLines) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    else GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                    // RENDER ALL OBJECTS
                    {
                        GL.DepthMask(true);

                        GL.Enable(EnableCap.CullFace);
                        GL.Disable(EnableCap.Blend);
                        GL.CullFace(CullFaceMode.Back);
                        lock (allRenderers)
                        {

                            for (int i = 0; i < allRenderers.Count; i++)
                            {
                                var renderer = allRenderers[i];
                                if (renderer == null) continue;

                                if (renderer.ShouldRenderGeometry)
                                {
                                    if (renderer.AllowsFrustumCulling == false || GeometryUtility.TestPlanesAABB(frustrumPlanes, renderer.bounds))
                                    {
                                        renderer.Material.BeforeBindCallback();
                                        renderer.Material.Uniforms.SendAllUniformsTo(renderer.Material.GBufferShader.Uniforms);
                                        renderer.Material.GBufferShader.Bind();
                                        renderer.UploadUBOandDraw(camera, ubo);
                                        countMeshesRendered++;

                                        if (renderer.AllowsFrustumCulling == false) renderer.SetCameraRenderStatus(camera, Renderer.RenderStatus.RenderedForced);
                                        else renderer.SetCameraRenderStatus(camera, Renderer.RenderStatus.RenderedAndVisible);
                                    }
                                    else
                                    {
                                        renderer.SetCameraRenderStatus(camera, Renderer.RenderStatus.NotRendered);
                                    }
                                }
                            }
                        }

                    }

                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                }

                #region Lights rendering

                var allLights = scene.Lights;
                lock (allLights)
                {
                    for (int lightIndex = 0; lightIndex < allLights.Count; lightIndex++)
                    {
                        var light = allLights[lightIndex];
                        if (light == null) continue;

                        var shadowMap = light.ShadowMap;

                        // SHADOW MAAPING
                        if (shadowsEnabled && light.HasShadows)
                        {

                            //GL.Enable(EnableCap.CullFace);
                            //GL.CullFace(CullFaceMode.Back);

                            shadowMap.FrameBufferForWriting();

                            GL.Enable(EnableCap.DepthTest);
                            GL.DepthMask(true);

                            shadowMap.Clear();



                            shadowMap.shadowViewCamera.UploadDataToUBO(ubo);


                            for (int i = 0; i < allRenderers.Count; i++)
                            {
                                var renderer = allRenderers[i];
                                if (renderer == null) continue;

                                //if (renderer.CanBeFrustumCulled == false || GeometryUtility.TestPlanesAABB(frustrumPlanes, renderer.bounds)) 
                                {
                                    renderer.Material.BeforeBindCallback();
                                    renderer.Material.Uniforms.SendAllUniformsTo(renderer.Material.DepthGrabShader.Uniforms);
                                    renderer.Material.DepthGrabShader.Bind();
                                    renderer.UploadUBOandDraw(shadowMap.shadowViewCamera, ubo);
                                }
                            }

                        }


                        camera.UploadDataToUBO(ubo); // bind camera view params

                        // G BUFFER LIGHT PASS

                        {
                            GL.Disable(EnableCap.CullFace);
                            //GL.CullFace(CullFaceMode.Back);

                            GL.Disable(EnableCap.DepthTest);
                            GL.DepthMask(false);


                            light.UploadUBOdata(ubo, lightIndex);

                            var shader = Factory.GetShader("internal/deferred.oneLight.shader");
                            gBuffer.BindForLightPass(shader);
                            if (shadowsEnabled && light.HasShadows)
                            {
                                shadowMap.BindUniforms(shader);
                            }

                            shader.Bind();

                            GL.Enable(EnableCap.Blend);
                            //GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
                            //GL.BlendFunc(BlendingFactorSrc.SrcColor, BlendingFactorDest.SrcColor);                    
                            GL.BlendEquation(BlendEquationMode.FuncAdd);
                            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
                            quadMesh.Draw();
                            GL.Disable(EnableCap.Blend);

                        }

                    }

                }

                #endregion

            }

            if (drawLines)  GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            // POST PROCESS EFFECTs
            if (enablePostProcessEffects)
            {
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);

                GL.Disable(EnableCap.DepthTest);
                GL.DepthMask(false);

                foreach (var pe in camera.postProcessEffects)
                {
                    if (pe.IsEnabled == false) continue;
                    pe.BeforeBindCallBack();
                    gBuffer.BindForPostProcessEffects(pe);
                    pe.Shader.Bind();
                    quadMesh.Draw();
                }
            }


            // FINAL DRAW TO SCREEN
            {
                //DebugDrawTexture(gBuffer.finalTextureToRead);

                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);

                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Viewport(0, 0, Width, Height);
                //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

                finalDrawShader.Uniforms.Set("finalDrawTexture", gBuffer.finalTextureToRead);
                finalDrawShader.Bind();

                quadMesh.Draw();
            }

            /*if(debugBounds)
            {
                var allColiders = new List<BoxCollider>();
                foreach (var go in Factory.allEntitys)
                {
                    allColiders.AddRange(go.GetComponents<BoxCollider>());
                }
                GL.DepthMask(false);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);
                foreach (var c in allColiders)
                {
                    var modelMat = c.entity.transform.GetScalePosRotMatrix();
                    var modelViewMat = modelMat * camera.GetViewMat();
                    ubo.model.modelMatrix = modelMat;
                    ubo.model.modelViewMatrix = modelViewMat;
                    ubo.model.modelViewProjectionMatrix = modelViewMat * camera.GetProjectionMat();
                    ubo.modelUBO.UploadData();
                    skyboxMesh.Draw();
                }
            }*/
            if (drawGBufferContents) gBuffer.DebugDrawContents();
            //if (drawShadowMapContents) DebugDrawTexture(shadowMap.depthMap, new Vector4(0.5f, 0.5f, 1, 1), new Vector4(0.5f,0.5f,0,1), 1, 0);


            /*
            {
                var shader = Factory.GetShader("internal/forward.allLights.shader");
                gBuffer.BindForWriting();

                // transparent pass, must enable blending
                GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(false);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


                shader.Bind();
                //shader.SetParam(Shader.screenSizeLocationName, new Vector2(Width, Height));
                camera.BindUniforms(shader);
                //Light.BindAll(shader);

                foreach (var go in Factory.allEntitys)
                {
                    foreach (var renderer in go.GetComponents<Renderer>())
                    {
                        renderer.Draw(shader, camera);
                    }
                }
            }*/


            SwapBuffers();

            Debug.AddValue("countMeshesRendered", countMeshesRendered + "/" + allRenderers.NonNullCount);

        }

    }
}