using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.Windows.Data;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using DbViewport = Autodesk.AutoCAD.DatabaseServices.Viewport;

[assembly: AssemblyTitle("AutoCADGame")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Youzi, Inc.")]
[assembly: AssemblyProduct("AutoCADGame")]
[assembly: AssemblyCopyright("Copyright © Youzi, Inc. 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("a3d338bd-63bd-46e1-8534-05fe882db9e6")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: ExtensionApplication(typeof(AutoCADGame.App))]
[assembly: CommandClass(typeof(AutoCADGame.Commands))]

namespace AutoCADGame
{
    enum KeyDirection { Null, Up, Down, Left, Right }

    class C
    {
        static public int AreaLimit = 30;
        static public int HalfAreaLimit { get { return AreaLimit / 2; } }
        static public string QatBtnName = "MyQatButton";
        static public string QatBtnText = "Back to AutoCAD";
        static public string SmallImageExit = "ExitSmall.PNG";
        static public string CuiName = "AutoCADGame";
        static public string DataFile = "data.dwg";
        static public int EnemyCount = 20;
        static public int ColorCount = 20;
        static public double MainSpriteAngleStep = 5;
        static public double MainSpriteSpeedStep = 0.3;
        static public int MainSpriteLife = 3;
        static public int MainSpriteBullets = 100;
        static public double ViewAngleStep = 1;
        static public double ViewMoveStep = 0.2;
        static public int Fps = 20;
        static public int SubDrawMode = 128;
        static public int ViewportNum = 2;
        static public string NsDot = "AutoCADGame.";
        static public int MaxColor = 220;
        static public int D360 = 360;
        static public double EnemyMaxSpeed = 1.5;
        static public double MainSpriteMaxSpeed = 1.8;
        static public double BulletSpeed = 10;
        static public double BulletMaxSpeed = 100;
        static public double EnemyRadius = 0.5;
        static public double MainSpriteRadius = 1;
        static public double ZoomFactor = 1;
        static public double CurrentZoomFactor = 1;
        static public int Kilo = 1000;
        static public double GridUnit = 0.5;
    }

    public class Commands
    {
        [CommandMethod("Gogogo")]
        public void Gogogo()
        {
            try
            {
                CuiInit();
                RecursiveFolderHelper.ProcessDirectory(G.DllPath, pCopyPngFiles);

                AcadApp.SetSystemVariable("LIMMAX", new Point2d(C.HalfAreaLimit, C.HalfAreaLimit));
                AcadApp.SetSystemVariable("LIMMIN", new Point2d(-C.HalfAreaLimit, -C.HalfAreaLimit));
                AcadApp.SetSystemVariable("GRIDUNIT", new Point2d(C.GridUnit, C.GridUnit));

                G.Doc.SendStringToExecute("TILEMODE\n1\n", false, false, true);
                G.Doc.SendStringToExecute("VSCURRENT\n2d\n", false, false, true);
                G.Doc.SendStringToExecute("GRIDMODE\n0\n", false, false, true);
                G.Doc.SendStringToExecute("Zoom\nS\n2\n", false, false, true);
                G.Doc.SendStringToExecute("VSCURRENT\nR\n", false, false, true);
                G.Doc.SendStringToExecute("CLEANSCREENON\n", false, false, true);
                G.Doc.SendStringToExecute("UCSICON\nOFF\n", false, false, true);
                G.Doc.SendStringToExecute("StatusBar\n0\n", false, false, true);
                
                if (AcadApp.Version.Major >= 18 && AcadApp.Version.Minor > 0)
                    G.Doc.SendStringToExecute("GRIDMODE\n1\n", false, false, true);
                G.Doc.SendStringToExecute("NAVVCUBE\nOFF\n", false, false, true);
                G.Doc.SendStringToExecute("PERSPECTIVE\n1\n", false, false, true);
                G.Doc.SendStringToExecute("FRAME\n0\n", false, false, true);
                G.Doc.SendStringToExecute("IMAGEFRAME\n1\n", false, false, true); // cad2010 bug.
                G.Doc.SendStringToExecute("IMAGEFRAME\n0\n", false, false, true);
                G.Doc.SendStringToExecute("GameInitialize\n", false, false, true);
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("Command Gogogo failed by {0}", ex.Message));
            }
        }

        private void pCopyPngFiles(string file, ref bool stop)
        {
            file = file.Trim();
            FileInfo fi = new FileInfo(file);
            if (fi.Extension.ToLower() == ".png")
            {
                string dest = AcadApp.GetSystemVariable("DWGPREFIX") as string + fi.Name;
                if (!File.Exists(dest))
                    File.Copy(file, dest);
            }
        }

        void DocumentManager_DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            try
            {
                AcadApp.PreTranslateMessage -= new PreTranslateMessageEventHandler(
                    AcadApplication_PreTranslateMessage);
                Manager.Ins.EndGame();
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("DocumentDestroyed_unload failed by {0}", ex.Message));
            } 
            try
            {
                if (mPrevCuiFile.ToLower() == mCuiFile.ToLower())
                    Cui.RestoreToCui_ForDestroy(Cui.AcadCuiFile);
                else
                    Cui.RestoreToCui_ForDestroy(mPrevCuiFile);
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("DocumentDestroyed_restoreCui failed by {0}", ex.Message));
            }
        }

        [CommandMethod("StopGogogo")]
        public void StopGogogo()
        {
            try
            {
                AcadApp.PreTranslateMessage -= new PreTranslateMessageEventHandler(
                    AcadApplication_PreTranslateMessage);
                Manager.Ins.EndGame();
                if (mPrevCuiFile.ToLower() == mCuiFile.ToLower())
                    Cui.RestoreToCui(Cui.AcadCuiFile);
                else
                    Cui.RestoreToCui(mPrevCuiFile);
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("Command StopGogogo failed by {0}", ex.Message));
            }
        }

        [CommandMethod("GameInitialize")]
        public void GameInitialize()
        {
            try
            {
                AddQatButtons();

                UI.InitializeWindow iw = new UI.InitializeWindow();
                iw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                AcadApp.ShowModelessWindow(iw);

                Manager.Ins.Level = 1;
                Manager.Ins.Initialize();

                iw.Close();
                
                UI.HelpWindow w = new UI.HelpWindow();
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                w.ShowDialog();

                AcadApp.PreTranslateMessage += new PreTranslateMessageEventHandler(
                    AcadApplication_PreTranslateMessage);
                AcadApp.DocumentManager.DocumentDestroyed += new DocumentDestroyedEventHandler(
                    DocumentManager_DocumentDestroyed);
                Manager.Ins.StartGame();
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("GameInitialize failed by {0}", ex.Message));
            }
        }

        private void AddQatButtons()
        {
            Autodesk.Windows.RibbonButton btn = new Autodesk.Windows.RibbonButton();
            btn.Name = C.QatBtnName;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(Path.Combine(G.DllPath, C.SmallImageExit),
                UriKind.RelativeOrAbsolute);
            bi.EndInit();
            btn.Image = bi;
            btn.CommandHandler = new AcadCommand("StopGoGoGo ");
            btn.Text = C.QatBtnText;
            Autodesk.Windows.ComponentManager.QuickAccessToolBar.AddStandardItem(btn);
        }

        class AcadCommand : ICommand
        {
            public AcadCommand(string command) 
            {
                mCommand = command;
                if (false && CanExecuteChanged != null)
                    CanExecuteChanged(null, null);
            }
            public event EventHandler CanExecuteChanged;
            public bool CanExecute(object parameter) { return true; }
            public void Execute(object parameter)
            {
                if (G.Doc == null)
                    return;
                G.Doc.SendStringToExecute(mCommand, false, false, true);
            }
            private string mCommand;
        }

        private void CuiInit()
        {
            mPrevCuiFile = Cui.CurCuiFile;
            Cui c = new Cui(C.CuiName);
            c.ShowCommandLineWindow(false);
            c.Save();
            c.Load(false);
            mCuiFile = c.CuiFile;
        }

        void AcadApplication_PreTranslateMessage(object sender, PreTranslateMessageEventArgs e)
        {
            try
            {
                bool isHandled = false;
                Native.PreTranslateAppMessage(e.Message, ref isHandled);
                e.Handled = isHandled;
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("PreTranslateMessage failed by {0}", ex.Message));
            }
        }
        private string mPrevCuiFile;
        private string mCuiFile;
    }

    public class NotifiedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class Manager : NotifiedBase
    {
        public static Manager Ins
        {
            get
            {
                if (sInstance == null)
                    sInstance = new Manager();
                return sInstance;
            }
        }

        public List<Sprite> Enemys { get { return mEnemySprites; } }

        public List<Sprite> Bullets { get { return mBullets; } }

        public Sprite MainSprite { get { return mMainSprite; } }

        public int Level 
        {
            get
            {
                return mLevel;
            }
            set
            {
                mLevel = value;
                if (value == 1)
                {
                    C.MainSpriteLife = 1;
                    C.EnemyCount = 1;
                    C.MainSpriteBullets = 3;
                    C.EnemyMaxSpeed = 0.5;
                    C.MainSpriteMaxSpeed = 1.2;
                    C.MainSpriteSpeedStep = 1.2;
                    C.ZoomFactor = 0.6;
                    C.AreaLimit = 16;
                    C.ViewAngleStep = 1;
                    C.ViewMoveStep = 0.2;
                }
                if (value == 2)
                {
                    C.MainSpriteLife = 2;
                    C.EnemyCount = 10;
                    C.MainSpriteBullets = 30;
                    C.EnemyMaxSpeed = 1.0;
                    C.MainSpriteMaxSpeed = 1.2;
                    C.MainSpriteSpeedStep = 1.2;
                    C.ZoomFactor = 0.8;
                    C.AreaLimit = 16;
                    C.ViewAngleStep = 1;
                    C.ViewMoveStep = 0.2;
                }
                if (value == 3)
                {
                    C.MainSpriteLife = 3;
                    C.EnemyCount = 20;
                    C.MainSpriteBullets = 60;
                    C.EnemyMaxSpeed = 1.5;
                    C.MainSpriteMaxSpeed = 1.8;
                    C.MainSpriteSpeedStep = 1.8;
                    C.ZoomFactor = 1;
                    C.AreaLimit = 30;
                    C.ViewAngleStep = 3;
                    C.ViewMoveStep = 0.5;
                }
                if (value == 4)
                {
                    C.MainSpriteLife = 4;
                    C.EnemyCount = 40;
                    C.MainSpriteBullets = 120;
                    C.EnemyMaxSpeed = 1.5;
                    C.MainSpriteMaxSpeed = 2;
                    C.MainSpriteSpeedStep = 2;
                    C.ZoomFactor = 1.2;
                    C.AreaLimit = 50;
                    C.ViewAngleStep = 5;
                    C.ViewMoveStep = 0.8;
                }
                if (value == 5)
                {
                    C.MainSpriteLife = 5;
                    C.EnemyCount = 80;
                    C.MainSpriteBullets = 300;
                    C.EnemyMaxSpeed = 1.5;
                    C.MainSpriteMaxSpeed = 2;
                    C.MainSpriteSpeedStep = 2;
                    C.ZoomFactor = 1.2;
                    C.AreaLimit = 65;
                    C.ViewAngleStep = 5;
                    C.ViewMoveStep = 0.8;
                }
            }
        }

        public int Fps { get; private set; }
        
        public void Initialize()
        {
            sStartTickCount = Environment.TickCount;
            sCurrentTickCount = sStartTickCount;
            mEnemySprites = new List<Sprite>();
            mBullets = new List<Sprite>();
            mExplodeEffects = new List<ViewSprite>();
            mMainSpriteLife = C.MainSpriteLife;
            mViewDirection = KeyDirection.Null;
            mMainSpriteBullets = C.MainSpriteBullets;
            mbFire = false;

            using (Transaction tr = G.Db.TransactionManager.StartTransaction())
            {
                pClearScreen(tr);
                using (ViewTableRecord view = G.Ed.GetCurrentView())
                {
                    ViewHelper.Init(view);
                    mMainSprite = new Sprite();
                    mMainSprite.Init(tr, "MainSpriteController", view.Target, "ball", true);
                    for (int i = 0; i < C.EnemyCount; i++)
                    {
                        Sprite s = new Sprite();
                        s.Init(tr, "EnemySpriteController", Point3d.Origin, "enemy", true);
                        mEnemySprites.Add(s);
                    }
                }
                mInfoArea = new InfomationArea();
                mInfoArea.Init(tr);
                tr.Commit();
            }
        }

        public void StartGame()
        {
            mIdleTimer = new DispatcherTimer(); //the idle event is not reliable, timer instead
            mIdleTimer.Interval = new TimeSpan(0, 0, 0, 0, C.Kilo / C.Fps);
            mIdleTimer.Tick += new EventHandler(idleTimer_Tick);
            mIdleTimer.Start();
        }

        void idleTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Manager.Ins.Idle();
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("idleTimer_Tick failed by {0}", ex.Message));
            }
        }

        public void EndGame()
        {
            mIdleTimer.Stop();
        }

        public void Move(KeyDirection dir)
        {
            if (G.Doc == null)
                return;
            if (dir == KeyDirection.Null)
                return;
            ((MainSpriteController)mMainSprite.Controller).Move(dir, C.MainSpriteAngleStep,
                C.MainSpriteSpeedStep);
        }

        public void MoveView(KeyDirection dir)
        {
            mViewDirection = dir;
        }

        public void Fire()
        {
            mbFire = true;
        }

        public void Idle()
        {
            int curTickCount = Environment.TickCount;
            if (curTickCount - sCurrentTickCount > C.Kilo / C.Fps)
            {
                pRun((curTickCount - sCurrentTickCount) / (double)C.Kilo);
                G.Ed.UpdateScreen();
                AcadApp.UpdateScreen();
                Fps = C.Kilo / (curTickCount - sCurrentTickCount);
                NotifyPropertyChanged("Fps");
                G.Ed.WriteMessage(string.Format("\nfps:{0}", Fps));
                sCurrentTickCount = curTickCount;
            }
        }

        private void pRun(double deltaTime)
        {
            if (G.Doc == null)
                return;
            using (G.Doc.LockDocument())
            {
                using (Transaction tr = G.Db.TransactionManager.StartTransaction())
                {
                    if (mMainSpriteLife <= 0 && mExplodeEffects.Count <= 0)
                    {
                        tr.Abort();
                        pGameOverDialog();
                        return;
                    }
                    if (mEnemySprites.Count == 0 && mExplodeEffects.Count <= 0)
                    {
                        tr.Abort();
                        pNextLevelDialog();
                        return;
                    }
                    pMoveView();
                    pFire(tr);
                    mInfoArea.Run(tr, deltaTime);
                    mMainSprite.Controller.Run();
                    mMainSprite.Run(tr, deltaTime);

                    List<Sprite> invalidEnemys = new List<Sprite>();
                    List<Sprite> invalidBullets = new List<Sprite>();
                    mInvalidExplodes = new List<ViewSprite>();
                    foreach (Sprite s in mEnemySprites)
                    {
                        s.Controller.Run();
                        Sprite collidedBullet = 
                            ((EnemySpriteController)s.Controller).CheckCollideBullet();
                        if (collidedBullet != null)
                        {
                            invalidEnemys.Add(s);
                            invalidBullets.Add(collidedBullet);
                            pAddExplodeEffect(tr, s.Position);
                        }
                        else
                        {
                            if (((EnemySpriteController)s.Controller).CheckCollideMainSprite())
                            {
                                invalidEnemys.Add(s);
                                pAddExplodeEffect(tr, s.Position);
                                mMainSpriteLife--;
                            }
                            else
                                s.Run(tr, deltaTime);
                        }
                    }
                    foreach (Sprite s in mBullets)
                    {
                        if (s.Controller.Run())
                            s.Run(tr, deltaTime);
                        else
                            invalidBullets.Add(s);
                    }
                    if (mExplodeEffects.Count > 0)
                    {
                        foreach (ViewSprite s in mExplodeEffects)
                            s.Run(tr, deltaTime);
                    }
                    foreach (Sprite s in invalidBullets)
                        pDeleteSprite(tr, mBullets, s);
                    foreach (Sprite s in invalidEnemys)
                        pDeleteSprite(tr, mEnemySprites, s);
                    foreach (ViewSprite s in mInvalidExplodes)
                        mExplodeEffects.Remove(s);
                    mInfoArea.SetFpsText(string.Format("fps:{0}", Fps));
                    mInfoArea.SetMainText(string.Format("Life:{0}\\P{{\\C2;Bullets:{1}}}", 
                        mMainSpriteLife, mMainSpriteBullets));
                    mInfoArea.SetEnemyText(string.Format("{0}", mEnemySprites.Count));

                    tr.Commit();
                }
            }
        }

        public void Restart()
        {
            Level = 1;
            Initialize();
        }

        private void pClearScreen(Transaction tr)
        {
            BlockTable bt = (BlockTable)tr.GetObject(G.Db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            foreach (ObjectId id in ms)
            {
                Entity e = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                if (e != null)
                    e.Erase();
            }
            Autodesk.AutoCAD.DatabaseServices.Polyline bound = new
                Autodesk.AutoCAD.DatabaseServices.Polyline();
            bound.AddVertexAt(0, new Point2d(-C.HalfAreaLimit, -C.HalfAreaLimit), 0, 0, 0);
            bound.AddVertexAt(1, new Point2d(-C.HalfAreaLimit, C.HalfAreaLimit), 0, 0, 0);
            bound.AddVertexAt(2, new Point2d(C.HalfAreaLimit, C.HalfAreaLimit), 0, 0, 0);
            bound.AddVertexAt(3, new Point2d(C.HalfAreaLimit, -C.HalfAreaLimit), 0, 0, 0);
            bound.Closed = true;
            ms.AppendEntity(bound);
            tr.AddNewlyCreatedDBObject(bound, true);
        }

        private void pAddExplodeEffect(Transaction tr, Point3d pos)
        {
            ViewSprite explode = new ViewSprite();
            explode.SelfErased += new Action<SpriteBase>(pExplode_SelfErased);
            explode.Init(tr, string.Empty, pos, "bang", false);
            mExplodeEffects.Add(explode);
        }

        private void pFire(Transaction tr)
        {
            if (!mbFire)
                return;
            Sprite s = new Sprite();
            string ctrl = "SmallBulletController";
            if (mMainSpriteBullets >= 0)
                ctrl = "BulletController";
            s.Init(tr, ctrl, mMainSprite.Position, "bullet", true);
            s.Position += mMainSprite.Direction * 1;
            s.DirectionAngle = mMainSprite.DirectionAngle;
            mBullets.Add(s);
            mMainSpriteBullets--;
            mbFire = false;
        }

        private void pDeleteSprite(Transaction tr, List<Sprite> list, Sprite s)
        {
            list.Remove(s);
            s.Erase(tr);
        }

        private void pExplode_SelfErased(SpriteBase obj)
        {
            mInvalidExplodes.Add(obj as ViewSprite);
        }

        private void pNextLevelDialog()
        {
            Level++;
            UI.NextLevel n = new UI.NextLevel();
            n.Init(Level);
            n.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            n.ShowDialog();
            Initialize();
        }

        private void pGameOverDialog()
        {
            UI.GameOver o = new UI.GameOver();
            o.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            o.Init();
            o.ShowDialog();
            if (o.IsOk)
                Restart();
            else
                G.Doc.SendStringToExecute("StopGogogo\n", false, false, true);
        }

        private void pMoveView()
        {
            KeyDirection dir = mViewDirection;
            if (G.Doc == null)
                return;
            if (dir == KeyDirection.Null)
                return;
            using (Transaction tr = G.Db.TransactionManager.StartTransaction())
            {
                using (ViewTableRecord view = G.Ed.GetCurrentView())
                    ViewHelper.Move(view, dir, C.ViewAngleStep, C.ViewMoveStep);
                tr.Commit();
            }
            mViewDirection = KeyDirection.Null;
        }

        static int sStartTickCount;
        static int sCurrentTickCount;
        private DispatcherTimer mIdleTimer;
        private static Manager sInstance;
        private Sprite mMainSprite;
        private List<Sprite> mEnemySprites;
        private List<Sprite> mBullets;
        private List<ViewSprite> mExplodeEffects;
        private List<ViewSprite> mInvalidExplodes;
        private KeyDirection mViewDirection;
        private bool mbFire;
        private InfomationArea mInfoArea;
        private int mLevel;

        private int mMainSpriteLife;
        private int mMainSpriteBullets;
    }

    class InfomationArea
    {
        public void Init(Transaction tr)
        {
            mMainTitle = new FixedPosViewSprite();
            mMainTitle.Init(tr, string.Empty, 60, 60, "chicken", true);
            mEnemyTitle = new FixedPosViewSprite();
            mEnemyTitle.Init(tr, string.Empty, 150, 60, "badEgg", false);
            mTextForMain = new TextObject();
            mTextForMain.Init(tr, 40, 130, string.Empty, Color.Red);
            mTextForEnemy = new TextObject();
            mTextForEnemy.Init(tr, 135, 95, string.Empty, Color.Blue);
            mTextForFps = new TextObject();
            mTextForFps.Init(tr, 200, 5, "FPS", Color.Green);
        }
        public void Run(Transaction tr, double deltaTime)
        {
            mMainTitle.Run(tr, deltaTime);
            mEnemyTitle.Run(tr, deltaTime);
            mTextForMain.Run(tr);
            mTextForEnemy.Run(tr);
            mTextForFps.Run(tr);
        }
        public void SetMainText(string t)
        {
            mTextForMain.Text = t;
        }
        public void SetEnemyText(string t)
        {
            mTextForEnemy.Text = t;
        }
        public void SetFpsText(string t)
        {
            mTextForFps.Text = t;
        }
        private FixedPosViewSprite mMainTitle;
        private FixedPosViewSprite mEnemyTitle;
        private TextObject mTextForMain;
        private TextObject mTextForEnemy;
        private TextObject mTextForFps;
    }

    class TextObject
    {
        public string Text { get; set; }

        public void Init(Transaction tr, int x, int y, string text, Color clr)
        {
            MText mt = new MText();
            mt.SetDatabaseDefaults();
            Text = text;
            mt.Contents = text;
            mt.Color = Autodesk.AutoCAD.Colors.Color.FromColor(clr);
            mt.TextHeight = 0.25;
            mPt.X = x;
            mPt.Y = y;
            using (ViewTableRecord view = G.Ed.GetCurrentView())
            {
                mt.Location = view.Target;
                mt.TransformBy(ViewHelper.GetDcs2Wcs(view));
                mt.TransformBy(Matrix3d.Scaling(C.CurrentZoomFactor, view.Target));
            }
            mt.Location = ViewHelper.Screen2World(x, y);
            BlockTable bt = (BlockTable)tr.GetObject(G.Db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            mId = ms.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
        }

        public void Run(Transaction tr)
        {
            if (!mId.IsValid)
                return;
            MText mt = tr.GetObject(mId, OpenMode.ForWrite) as MText;
            if (mt == null)
                return;
            mt.Contents = Text;
            using (ViewTableRecord view = G.Ed.GetCurrentView())
            {
                mt.Location = view.Target;
                mt.Normal = Vector3d.ZAxis;
                mt.TransformBy(ViewHelper.GetDcs2Wcs(view));
                mt.Location = ViewHelper.Screen2World(mPt.X, mPt.Y);
            }
        }
        private System.Drawing.Point mPt;
        private ObjectId mId;
    }

    class TransientBlockObject // not being used for now
    {
        public virtual void Init(Transaction tr, string block)
        {
            ObjectId blockId = BlockHelper.GetBlockId(true, block,
                Path.Combine(G.DllPath, C.DataFile), tr);
            mBlockRef = new BlockReference(Point3d.Origin, blockId);
            TransientManager.CurrentTransientManager.AddTransient(mBlockRef,
                TransientDrawingMode.DirectShortTerm, C.SubDrawMode, new IntegerCollection() 
                { C.ViewportNum });
        }
        public void Update()
        {
            TransientManager.CurrentTransientManager.UpdateTransient(mBlockRef,
                new IntegerCollection() { C.ViewportNum });
        }
        public void Unload()
        {
            if (mBlockRef == null)
                return;
            try
            {
                TransientManager.CurrentTransientManager.EraseTransient(mBlockRef,
                    new IntegerCollection() { C.ViewportNum });
            }
            catch { }
            mBlockRef.Dispose();
        }
        private BlockReference mBlockRef;
    }

    class SpriteBase
    {
        public event Action<SpriteBase> SelfErased;

        public Point3d Position { get; set; }

        public Autodesk.AutoCAD.Colors.Color Color { get; set; }

        public Controller Controller { get { return mController; } }

        public ObjectId BlockObjectId { get { return mBlockRefObjectId; } }

        public virtual void Init(Transaction tr, string controller, Point3d pos, string block, 
            bool loopFrame)
        {
            mBlockRefObjectId = BlockHelper.InsertBlockref(Path.Combine(G.DllPath, C.DataFile), 
                block, pos, tr);
            Position = pos;
            BlockReference br = pGetBlockRef(tr);
            if (br == null)
                return;
            mFrames = BlockHelper.GetVisibilities(br);
            mCurrentFrame = 0;
            mLoopFrame = loopFrame;
            if (!string.IsNullOrEmpty(controller))
            {
                mController = Activator.CreateInstance(Type.GetType(C.NsDot + controller))
                    as Controller;
                mController.Init(this);
            }
        }

        public virtual void Run(Transaction tr, double deltaTime)
        {
            BlockReference br = pGetBlockRef(tr);
            if (br == null)
                return;
            if (mFrames != null)
            {
                if ((mCurrentFrame == mFrames.Count - 1) && !mLoopFrame)
                {
                    br.Erase();
                    if (SelfErased != null)
                        SelfErased(this);
                }
            }
            if (Color != null)
                br.Color = Color;
            br.Position = Position;
            pUpdateFrame(br);
        }

        public void Erase(Transaction tr)
        {
            BlockReference br = tr.GetObject(mBlockRefObjectId, OpenMode.ForWrite)
                as BlockReference;
            if (br == null)
                return;
            br.Erase();
        }

        protected void pUpdateFrame(BlockReference br)
        {
            if (br == null)
                return;

            if (mFrames != null)
            {
                if (mFrames.Count > 0)
                {
                    BlockHelper.SetVisibility(br, mFrames[mCurrentFrame]);
                    mCurrentFrame++;
                    if (mCurrentFrame > mFrames.Count - 1)
                        mCurrentFrame = mLoopFrame ? 0 : mFrames.Count - 1;
                }
            }
        }

        protected BlockReference pGetBlockRef(Transaction tr)
        {
            if (!mBlockRefObjectId.IsValid)
                return null;
            return tr.GetObject(mBlockRefObjectId, OpenMode.ForWrite)
                as BlockReference;
        }

        private ObjectId mBlockRefObjectId;
        private List<string> mFrames;
        private int mCurrentFrame;
        private bool mLoopFrame;
        private Controller mController;
    }

    class ViewSprite : SpriteBase
    {
        public override void Init(Transaction tr, string controller, Point3d pos, string block, 
            bool loopFrame)
        {
            using (ViewTableRecord view = G.Ed.GetCurrentView())
            {
                base.Init(tr, controller, view.Target, block, loopFrame);
                BlockReference br = pGetBlockRef(tr);
                if (br == null)
                    return;
                br.TransformBy(ViewHelper.GetDcs2Wcs(view));
                pOtherTransform(br, view);
                Position = pos;
                br.Position = Position;
            }
        }
        public override void Run(Transaction tr, double deltaTime)
        {
            using (ViewTableRecord view = G.Ed.GetCurrentView())
            {
                base.Run(tr, deltaTime);
                BlockReference br = pGetBlockRef(tr);
                if (br == null)
                    return;
                br.Position = view.Target;
                br.Normal = Vector3d.ZAxis;
                br.TransformBy(ViewHelper.GetDcs2Wcs(view));
                br.Position = Position;
            }
        }
        protected virtual void pOtherTransform(BlockReference br, ViewTableRecord view)
        {
        }
    }

    class FixedPosViewSprite : ViewSprite
    {
        public void Init(Transaction tr, string controller, int x, int y, string block,
            bool loopFrame)
        {
            mPt.X = x;
            mPt.Y = y;
            base.Init(tr, controller, ViewHelper.Screen2World(x, y), block, loopFrame);
        }

        protected override void pOtherTransform(BlockReference br, ViewTableRecord view)
        {
            br.TransformBy(Matrix3d.Scaling(C.CurrentZoomFactor, view.Target));
        }

        public override void Run(Transaction tr, double deltaTime)
        {
            Position = ViewHelper.Screen2World(mPt.X, mPt.Y);
            base.Run(tr, deltaTime);
        }

        private System.Drawing.Point mPt;
    }

    class Sprite : SpriteBase
    {
        public double DirectionAngle { get; set; }

        public double Speed { get; set; }

        public double Accel { get; set; }

        public double MaxSpeed { get; set; }

        public Vector3d Direction
        {
            get
            {
                Vector3d dir = Vector3d.XAxis;
                dir = dir.RotateBy(G.Degree2Radian(DirectionAngle), Vector3d.ZAxis);
                return dir.GetNormal();
            }
        }

        public override void Init(Transaction tr, string controller, Point3d pos, string block, 
            bool loopFrame)
        {
            Accel = 0;
            Speed = 0;
            base.Init(tr, controller, pos, block, loopFrame);
        }

        public override void Run(Transaction tr, double deltaTime)
        {
            base.Run(tr, deltaTime);

            double oldSpeed = Speed;
            Speed += Accel * deltaTime;
            if (Speed > MaxSpeed)
                Speed = MaxSpeed;
            if (Speed < -MaxSpeed)
                Speed = -MaxSpeed;
            Position += Direction * (Speed + oldSpeed) * deltaTime / 2;

            BlockReference br = pGetBlockRef(tr);
            if (br == null)
                return;
            br.Position = Point3d.Origin;
            br.Rotation = 0;
            DirectionAngle = DirectionAngle % C.D360;
            br.TransformBy(Matrix3d.Rotation(G.Degree2Radian(DirectionAngle), Vector3d.ZAxis,
                Point3d.Origin));
            br.Position = Position;
        }
    }

    class Controller 
    {
        public virtual SpriteBase SpriteBase { get; protected set; }
        public virtual void Init(SpriteBase s) { SpriteBase = s; }
        public virtual bool Run() { return true; }
        static protected void pEnsureBoundary(Sprite s)
        {
            if (s.Position.X > C.HalfAreaLimit && s.Direction.X > 0)
            {
                s.DirectionAngle += C.D360 / 4;
                if (s.Direction.X > 0)
                    s.DirectionAngle += C.D360 / 2;
            }
            if ((s.Position.X < -C.HalfAreaLimit) && s.Direction.X < 0)
            {
                s.DirectionAngle += C.D360 / 4;
                if (s.Direction.X < 0)
                    s.DirectionAngle += C.D360 / 2;
            }
            if (s.Position.Y > C.HalfAreaLimit && s.Direction.Y > 0)
            {
                s.DirectionAngle += C.D360 / 4;
                if (s.Direction.Y > 0)
                    s.DirectionAngle += C.D360 / 2;
            }
            if ((s.Position.Y < -C.HalfAreaLimit) && s.Direction.Y < 0)
            {
                s.DirectionAngle += C.D360 / 4;
                if (s.Direction.Y < 0)
                    s.DirectionAngle += C.D360 / 2;
            }
        }
    }

    class MainSpriteController : Controller
    {
        public Sprite Sprite { get { return SpriteBase as Sprite; } }

        public override void Init(SpriteBase s)
        {
            base.Init(s);
            Sprite.MaxSpeed = C.MainSpriteMaxSpeed;
        }
        public override bool Run()
        {
            pEnsureBoundary(Sprite);
            return true; 
        }
        public void Move(KeyDirection dir, double angleStep, double speedStep)
        {
            if (dir == KeyDirection.Null)
                return;
            else if (dir == KeyDirection.Left)
                Sprite.DirectionAngle += angleStep;
            else if (dir == KeyDirection.Right)
                Sprite.DirectionAngle -= angleStep;
            else if (dir == KeyDirection.Up)
                Sprite.Speed += speedStep;
            else if (dir == KeyDirection.Down)
                Sprite.Speed -= speedStep;
        }
    }

    class EnemySpriteController : Controller
    {
        public Sprite Sprite { get { return SpriteBase as Sprite; } }

        public override void Init(SpriteBase s)
        {
            base.Init(s);
            Sprite.Accel = (G.Rand(100) / 100.0);
            Sprite.Speed = (G.Rand(100) / 100.0);
            Sprite.DirectionAngle = G.Rand(C.D360);
            double x = G.Rand(C.HalfAreaLimit) - C.HalfAreaLimit / 2;
            if (x < 0)
                x -= C.HalfAreaLimit / 2;
            else
                x += C.HalfAreaLimit / 2;
            double y = G.Rand(C.HalfAreaLimit) - C.HalfAreaLimit / 2;
            if (y < 0)
                y -= C.HalfAreaLimit / 2;
            else
                y += C.HalfAreaLimit / 2;
            Sprite.Position = new Point3d(x, y, 0);
            Sprite.MaxSpeed = C.EnemyMaxSpeed;
            Sprite.Color = Autodesk.AutoCAD.Colors.Color.FromColor(Color.FromArgb(
                G.Rand(C.MaxColor), G.Rand(C.MaxColor), G.Rand(C.MaxColor)));
        }

        public Sprite CheckCollideBullet()
        {
            foreach (Sprite b in Manager.Ins.Bullets)
            {
                if (Sprite.Position.DistanceTo(b.Position) < C.EnemyRadius)
                    return b;
            }
            return null;
        }

        public bool CheckCollideMainSprite()
        {
            if (Manager.Ins.MainSprite.Position.DistanceTo(Sprite.Position) < 
                C.MainSpriteRadius + C.EnemyRadius)
                return true;
            return false;
        }

        public override bool Run()
        {
            Sprite.Accel = (G.Rand(100) / 100.0);
            pEnsureBoundary(Sprite);
            return true;
        }
    }

    class BulletController : Controller
    {
        public Sprite Sprite { get { return SpriteBase as Sprite; } }

        public override void Init(SpriteBase s)
        {
            base.Init(s);
            Sprite.Speed = C.BulletSpeed;
            Sprite.MaxSpeed = C.BulletMaxSpeed;
            Sprite.Color = Autodesk.AutoCAD.Colors.Color.FromColor(Color.FromArgb(
                G.Rand(C.MaxColor), G.Rand(C.MaxColor), G.Rand(C.MaxColor)));
        }

        public override bool Run()
        {
            if (Sprite.Position.X > C.HalfAreaLimit || Sprite.Position.X < -C.HalfAreaLimit)
                return false;
            else if (Sprite.Position.Y > C.HalfAreaLimit || Sprite.Position.Y < -C.HalfAreaLimit)
                return false;
            return true;
        }
    }

    class SmallBulletController : Controller // half speed, fixed distance (C.HalfAreaLimit / 2)
    {
        public Sprite Sprite { get { return SpriteBase as Sprite; } }
        public override void Init(SpriteBase s)
        {
            base.Init(s);
            Sprite.Speed = C.BulletSpeed / 2;
            Sprite.MaxSpeed = C.BulletMaxSpeed;
            Sprite.Color = Autodesk.AutoCAD.Colors.Color.FromColor(Color.Black);
            mBasePos = Sprite.Position;
        }
        public override bool Run()
        {
            if (Sprite.Position.X > C.HalfAreaLimit || Sprite.Position.X < -C.HalfAreaLimit)
                return false;
            else if (Sprite.Position.Y > C.HalfAreaLimit || Sprite.Position.Y < -C.HalfAreaLimit)
                return false;
            if (Sprite.Position.DistanceTo(mBasePos) >= C.HalfAreaLimit / 2)
                return false;
            return true;
        }
        private Point3d mBasePos;
    }

    static class Native
    {
        [DllImport("user32", EntryPoint = "GetParent")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll")]
        public static extern bool SetCursor(IntPtr hcur);

        private static IntPtr sViewWndHandle = IntPtr.Zero;
        private static Point3d sMousePositionWorld;
        private static System.Drawing.Point sMousePosition;
        private static IntPtr sCursor = IntPtr.Zero;

        public static void PreTranslateAppMessage(MSG msg, ref bool handled)
        {
            const int WM_MOUSEFIRST = 0x0200;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_MOUSELAST = 0x020E;
            const int WM_KEYDOWN = 0x0100;
            const ushort VK_LEFT = 0x25;
            const ushort VK_UP = 0x26;
            const ushort VK_RIGHT = 0x27;
            const ushort VK_DOWN = 0x28;
            const ushort VK_HOME = 0x24;
            const ushort VK_END = 0x23;
            const ushort VK_SPACE = 0x20;

            if (G.Doc == null)
                return;

            if (GetParent(msg.hwnd) == G.Doc.Window.Handle && sViewWndHandle == IntPtr.Zero)
                sViewWndHandle = msg.hwnd;

            if (msg.message >= WM_MOUSEFIRST && msg.message <= WM_MOUSELAST)
            {
                if (sViewWndHandle == IntPtr.Zero)
                    return;

                RECT rect;
                GetWindowRect(sViewWndHandle, out rect);
                System.Drawing.Point pt = new System.Drawing.Point(msg.pt_x, msg.pt_y);

                if (pt.X < rect.Left || pt.X > rect.Right
                    || pt.Y < rect.Top || pt.Y > rect.Bottom)
                    return;

                pt.X -= rect.Left;
                pt.Y -= rect.Top;

                if (msg.message == WM_MOUSEMOVE)
                {
                    sMousePosition = pt;
                    sMousePositionWorld = G.Ed.PointToWorld(pt, C.ViewportNum);

                    Point3d p = new Point3d(C.AreaLimit, -C.AreaLimit, C.HalfAreaLimit);
                    Line3d l = new Line3d(p, sMousePositionWorld);
                    Plane pl = new Plane(Point3d.Origin, Vector3d.ZAxis);
                    sMousePositionWorld = (l.IntersectWith(pl))[0];

                    if (sCursor == IntPtr.Zero)
                        sCursor = LoadCursorFromFile(G.GetFullFileName("cursor.ani"));
                    SetCursor(sCursor);
                }
                handled = true;
            }

            if (msg.message == WM_KEYDOWN)
            {
                ushort key = LOWORD((uint)msg.wParam);
                if (key == VK_LEFT || key == VK_DOWN || key == VK_RIGHT || key == VK_UP
                    || key == VK_HOME || key == VK_END)
                {
                    using (DocumentLock l = G.Doc.LockDocument())
                    {
                        KeyDirection dir = KeyDirection.Null;
                        if (key == VK_LEFT)
                            dir = KeyDirection.Left;
                        else if (key == VK_RIGHT)
                            dir = KeyDirection.Right;
                        else if (key == VK_UP)
                            dir = KeyDirection.Up;
                        else if (key == VK_DOWN)
                            dir = KeyDirection.Down;
                        Manager.Ins.Move(dir);
                    }
                }
                if (key == 'a' || key == 'A')
                    Manager.Ins.MoveView(KeyDirection.Left);
                if (key == 'd' || key == 'D')
                    Manager.Ins.MoveView(KeyDirection.Right);
                if (key == 'w' || key == 'W')
                    Manager.Ins.MoveView(KeyDirection.Up);
                if (key == 's' || key == 'S')
                    Manager.Ins.MoveView(KeyDirection.Down);
                if (key == VK_SPACE)
                    Manager.Ins.Fire();
                handled = true;
            }
        }

        public static ushort LOWORD(uint value)
        {
            return (ushort)(value & 0xFFFF);
        }
        public static ushort HIWORD(uint value)
        {
            return (ushort)(value >> 16);
        }
        public static byte LOWBYTE(ushort value)
        {
            return (byte)(value & 0xFF);
        }
        public static byte HIGHBYTE(ushort value)
        {
            return (byte)(value >> 8);
        }  
    }

    static class ViewHelper
    {
        public static Point3d Screen2World(int x, int y)
        {
            return G.Ed.PointToWorld(new System.Drawing.Point(x, y), C.ViewportNum);
        }

        public static Matrix3d GetWcs2Dcs(ViewTableRecord view)
        {
            return GetDcs2Wcs(view).Inverse();
        }

        public static Matrix3d GetDcs2Wcs(ViewTableRecord view)
        {
            Matrix3d matWcs2Dcs;
            matWcs2Dcs = Matrix3d.PlaneToWorld(view.ViewDirection);
            matWcs2Dcs = Matrix3d.Displacement(view.Target - Point3d.Origin) * matWcs2Dcs;
            matWcs2Dcs = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target)
                * matWcs2Dcs;
            return matWcs2Dcs;
        }

        public static void Init(ViewTableRecord view)
        {
            view.Target = Point3d.Origin;
            view.ViewDirection = new Vector3d(C.AreaLimit, -C.AreaLimit, C.HalfAreaLimit);
            view.CenterPoint = Point2d.Origin;
            view.RenderMode = ViewTableRecordRenderMode.GouraudShadedWithWireframe;
            view.Height = view.Height * (C.ZoomFactor / C.CurrentZoomFactor);
            view.Width = view.Width * (C.ZoomFactor / C.CurrentZoomFactor);
            C.CurrentZoomFactor = C.ZoomFactor;
            G.Ed.SetCurrentView(view);
        }

        public static void Move(ViewTableRecord view, KeyDirection dir, double degreeStep, 
            double distanceStep)
        {
            if (dir == KeyDirection.Left)
            {
                Vector3d v = view.ViewDirection;
                view.ViewDirection = v.TransformBy(Matrix3d.Rotation(
                    G.Degree2Radian(degreeStep * -1), Vector3d.ZAxis, Point3d.Origin));
                if (view.ViewDirection == v)
                    view.Target = new Point3d(
                        view.Target.X - distanceStep, view.Target.Y, view.Target.Z);
            }
            if (dir == KeyDirection.Right)
            {
                Vector3d v = view.ViewDirection;
                view.ViewDirection = v.TransformBy(Matrix3d.Rotation(
                    G.Degree2Radian(degreeStep), Vector3d.ZAxis, Point3d.Origin));
                if (view.ViewDirection == v)
                    view.Target = new Point3d(
                        view.Target.X + distanceStep, view.Target.Y, view.Target.Z);
            }
            if (dir == KeyDirection.Up)
            {
                Point3d oldTarget = view.Target;
                Vector3d v = view.ViewDirection;
                v = v.Negate().GetNormal() * distanceStep;
                view.Target = new Point3d(view.Target.X + v.X, view.Target.Y + v.Y, view.Target.Z);
                if (oldTarget == view.Target)
                    view.Target = new Point3d(
                        view.Target.X, view.Target.Y + distanceStep, view.Target.Z);
            }
            if (dir == KeyDirection.Down)
            {
                Point3d oldTarget = view.Target;
                Vector3d v = view.ViewDirection;
                v = v.GetNormal() * distanceStep;
                view.Target = new Point3d(view.Target.X + v.X, view.Target.Y + v.Y, view.Target.Z);
                if (oldTarget == view.Target)
                    view.Target = new Point3d(
                        view.Target.X, view.Target.Y - distanceStep, view.Target.Z);
            }
            G.Ed.SetCurrentView(view);
        }

        public static void Output(ViewTableRecord view)
        {
            if (G.Ed == null)
                return;
            G.Ed.WriteMessage(string.Format(
                @"view.Target: ({0},{1},{2}), 
                                        view.LensLength: {3},
                                        view.ViewDirection: {4},
                                        view.Height: {5},
                                        view.Width: {6},
                                        view.CenterPoint: ({7}, {8})",
                view.Target.X, view.Target.Y, view.Target.Z,
                view.LensLength, view.ViewDirection, view.Height, view.Width,
                view.CenterPoint.X, view.CenterPoint.Y));
        }
    }

    static class BlockHelper
    {
        public static List<string> GetVisibilities(BlockReference br)
        {
            if (br == null || !br.IsDynamicBlock)
                return null;
            List<string> list = null;
            DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty prop in pc)
            {
                if (prop.PropertyName == "Visibility1") // not general, only for my data
                {
                    foreach (object value in prop.GetAllowedValues())
                    {
                        string s = value as string;
                        if (!string.IsNullOrEmpty(s))
                        {
                            if (list == null)
                                list = new List<string>();
                            list.Add(s);
                        }
                    }
                }
            }
            return list;
        }

        public static string GetVisibility(BlockReference br)
        {
            if (br == null || !br.IsDynamicBlock)
                return null;
            DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty prop in pc)
            {
                if (prop.PropertyName == "Visibility1") // not general, only for my data
                    return (prop.Value as string);
            }
            return null;
        }

        public static void SetVisibility(BlockReference br, string name)
        {
            if (br == null || !br.IsDynamicBlock)
                return;
            DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty prop in pc)
            {
                if (prop.PropertyName == "Visibility1") // not general, only for my data
                {
                    prop.Value = name;
                    return;
                }
            }
        }

        public static ObjectId[] FindBlockRefIds(string blockName)
        {
            if (G.Doc == null)
                return null;
            TypedValue[] tvs = new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<and"),
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, blockName),
                new TypedValue((int)DxfCode.Operator, "and>") };
            SelectionFilter sf = new SelectionFilter(tvs);
            PromptSelectionResult psr = G.Ed.SelectAll(sf);
            if (psr.Status != PromptStatus.OK || psr.Value == null)
                return null;
            return psr.Value.GetObjectIds();
        }

        public static ObjectId FindFirstBlockRefId(string blockName)
        {
            ObjectId[] ids = FindBlockRefIds(blockName);
            if (ids == null)
                return ObjectId.Null;
            if (ids.Length == 0)
                return ObjectId.Null;
            return ids[0];
        }

        public static ObjectId InsertBlockref(string sourceFileName, string blockName, 
            Point3d pos, Transaction tr)
        {
            ObjectId blockRefId = ObjectId.Null;
            try
            {
                ObjectId blockId = GetBlockId(true, blockName, sourceFileName, tr);
                if (!blockId.IsValid)
                    return ObjectId.Null;
                blockRefId = pInsertBlockReference(pos, blockId, tr);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Debug.Fail(e.Message, e.StackTrace);
            }
            return blockRefId;
        }

        public static ObjectId GetBlockId(bool createIfNotExisting, string blockName,
            string sourceFile, Transaction tr)
        {
            if (string.IsNullOrEmpty(sourceFile) || string.IsNullOrEmpty(blockName))
                return ObjectId.Null;
            if (G.Doc == null)
                return ObjectId.Null;
            try
            {
                ObjectId blockId = ObjectId.Null;
                pBlockReadOnlyAction(G.Db, tr, blockName, btr => blockId = btr.Id);
                if (blockId.IsValid)
                    return blockId;
                if (!createIfNotExisting)
                    return ObjectId.Null;
                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(sourceFile, System.IO.FileShare.Read, true, string.Empty);
                    IdMapping mapping = new IdMapping();
                    ObjectIdCollection blockIds = new ObjectIdCollection();
                    pBlockReadOnlyAction(sourceDb, null, blockName, btr => blockIds.Add(btr.Id));
                    sourceDb.WblockCloneObjects(blockIds, G.Db.BlockTableId, mapping,
                        DuplicateRecordCloning.Ignore, false);
                    blockId = mapping[blockIds[0]].Value;
                    return blockId;
                }
            }
            catch (System.Exception e)
            {
                Debug.Assert(false, string.Format("Failed to copy block {0} from file {1} by {2}",
                    blockName, sourceFile, e.Message));
            }
            return ObjectId.Null;
        }

        private static void pBlockReadOnlyAction(Database db, Transaction tr, string blockName, 
            Action<BlockTableRecord> action)
        {
            if (action == null || string.IsNullOrEmpty(blockName))
                return;
            Transaction newTr = null;
            if (tr == null)
            {
                if (db == null)
                    return;
                newTr = db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead, 
                        false);
                    if (btr.Name == blockName)
                        action(btr);
                }
            }
        }

        private static ObjectId pInsertBlockReference(Point3d pos, ObjectId blockId, Transaction tr)
        {
            return TransHelper.TransResult<Point3d, ObjectId, ObjectId>
                (tr, pTransInsertBlockRef, pos, blockId);
        }

        private static ObjectId pTransInsertBlockRef(Transaction tr, Point3d pos, ObjectId blockId)
        {
            ObjectId brId = ObjectId.Null;
            BlockTable bt = (BlockTable)tr.GetObject(G.Db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            BlockReference br = new BlockReference(Point3d.Origin, blockId);
            brId = ms.AppendEntity(br);
            br.Position = pos;
            tr.AddNewlyCreatedDBObject(br, true);
            return brId;
        }
    }

    public class RecursiveFolderHelper
    {
        public delegate void FileHandleDelegate(string file, ref bool stop);

        public static void ProcessDirectories(string[] foldersOrFiles, FileHandleDelegate fileHandler)
        {
            foreach (string path in foldersOrFiles)
            {
                if (File.Exists(path))
                {
                    if (ProcessFile(path, fileHandler))
                        return;
                }
                else if (Directory.Exists(path))
                {
                    if (ProcessDirectory(path, fileHandler))
                        return;
                }
                else
                    Debug.Fail(string.Format("{0} is not a valid file or directory.", path));
            }
        }

        public static bool ProcessDirectory(string targetDirectory, FileHandleDelegate fileHandler)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                if (ProcessFile(fileName, fileHandler))
                    return true;
            }
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                if (ProcessDirectory(subdirectory, fileHandler))
                    return true;
            }
            return false;
        }

        public static bool ProcessFile(string path, FileHandleDelegate fileHandleDelegate)
        {
            if (fileHandleDelegate != null)
            {
                bool stop = false;
                fileHandleDelegate(path, ref stop);
                return stop;
            }
            return false;
        }
    }

    class Cui
    {
        public static Cui Current
        {
            get
            {
                if (s_curCui == null)
                {
                    s_curCui = new Cui();
                    s_curCui.mCs = Cui.CurCs;
                }
                return s_curCui;
            }
        }
        public Cui(string fileName)
        {
            mCuiFile = fileName;
        }
        public static void RestoreToAcadCui()
        {
            Reload(Cui.s_newMenuGroup, Cui.AcadCuiFile, true);
        }
        public static void RestoreToCui(string cuiFile)
        {
            Reload(Cui.s_newMenuGroup, cuiFile, true);
        }
        public static void RestoreToCui_ForDestroy(string cuiFile)
        {
            IConfigurationSection con = AcadApp.UserConfigurationManager.OpenCurrentProfile();
            using (con)
            {
                IConfigurationSection sec = con.CreateSubsection("General Configuration");
                using (sec)
                {
                    sec.WriteProperty("MenuFile", cuiFile);
                }
            }
            ClearWsProfile();
        }
        public static void ClearWsProfile()
        {
            IConfigurationSection con = AcadApp.UserConfigurationManager.OpenCurrentProfile();
            using (con)
            {
                IConfigurationSection sec2 =
                  con.CreateSubsection("General");
                using (sec2)
                {
                    sec2.WriteProperty("WSCURRENT", string.Empty);
                }
            }
        }
        public bool Save()
        {
            if (File.Exists(CuiFileWithExt))
                return Cs.IsModified ? Cs.Save() : true;
            else
                return Cs.SaveAs(CuiFileWithExt);
        }
        public void Load(bool showRibbon)
        {
            if (showRibbon)
                if (Cs.MenuGroup.RibbonRoot.RibbonTabSources.Count == 0)
                    showRibbon = false;
            if (this == Cui.Current)
            {
                if (CurIsAcad)
                    Reload("ACAD", "ACAD", showRibbon);
                else
                    Reload(Cui.s_newMenuGroup, Cui.CurCuiFile, showRibbon);
                return;
            }
            else
            {
                if (CurIsAcad)
                    Reload("ACAD", CuiFile, showRibbon);
                else
                    Reload(Cui.s_newMenuGroup, CuiFile, showRibbon);
            }
        }
        public bool AddMenuMacros(List<MacroData> macros)
        {
            StringCollection macroNames = new StringCollection();
            foreach (MacroData macro in macros)
                macroNames.Add(macro.Name);

            List<MenuMacro> tempList = Find<MenuMacro>("macro.Name", macroNames, 
                MacroGroup.MenuMacros);
            if (tempList != null && tempList.Count > 0)
            {
                G.Log("Duplicated macro names");
                return false;
            }

            foreach (MacroData macro in macros)
            {
                MenuMacro mm = new MenuMacro(MacroGroup, macro.Name, macro.Command, null);
                mm.macro.SmallImage = macro.SmallImage;
                mm.macro.LargeImage = macro.LargeImage;
            }
            return true;
        }
        public void RemoveMenuMacros(StringCollection macroNames)
        {
            List<MenuMacro> list = Find<MenuMacro>("macro.Name", macroNames, 
                MacroGroup.MenuMacros);
            Debug.Assert(list.Count == macroNames.Count);
            if (Remove<MenuMacro>(MacroGroup.MenuMacros, list))
                Cs.MenuGroup.SetModifiedItems(MenuGroup.ItemsCollection.MacroGroups);
        }

        public void RemoveMenu(MenuData menu)
        {
            StringCollection ids = new StringCollection();
            foreach (MenuData m in menu.Items)
                pGetPopMenus(m, ids);

            if (Remove<PopMenu>("ElementID", ids, Cs.MenuGroup.PopMenus))
                Cs.MenuGroup.SetModifiedItems(MenuGroup.ItemsCollection.PopMenus);

            foreach (Workspace ws in Cs.Workspaces)
            {
                if (Remove<WorkspacePopMenu>("PopMenuID", ids, ws.WorkspacePopMenus))
                    ws.SetIsModified();
            }
        }

        private void pGetPopMenus(MenuData menu, StringCollection ids)
        {
            if (menu.Items != null && menu.Items.Count > 0)
                ids.Add(menu.Id);
            foreach (MenuData m in menu.Items)
                pGetPopMenus(m, ids);
        }

        public void AddMenu(MenuData menu)
        {
            foreach (MenuData m in menu.Items)
            {
                PopMenu pm = pAddMenu(m, true);
                foreach (Workspace ws in Cs.Workspaces)
                {
                    ws.MenuBar = MenuBarToggle.on;
                    WorkspacePopMenu wspm = new WorkspacePopMenu(ws, pm);
                    wspm.Display = 1;
                    ws.SetIsModified();
                }
            }
            Cs.MenuGroup.SetModifiedItems(MenuGroup.ItemsCollection.PopMenus);
        }

        private PopMenu pAddMenu(MenuData m, bool bHasAlias)
        {
            PopMenu pm = new PopMenu(m.Name,
                bHasAlias ? new StringCollection() { m.Id } : null,
                m.Id, Cs.MenuGroup);
            int i = 0;
            foreach (MenuData mi in m.Items)
            {
                if (mi.Items != null && mi.Items.Count > 0)
                {
                    PopMenu subPm = pAddMenu(mi, false);
                    PopMenuRef menuRef = new PopMenuRef(subPm, pm, i);
                }
                else
                {
                    List<MenuMacro> list = Find<MenuMacro>("macro.Name",
                      new StringCollection() { mi.MacroName }, MacroGroup.MenuMacros);
                    if (list == null || list.Count == 0)
                        continue;
                    PopMenuItem pmi = new PopMenuItem(list[0], mi.Name, pm, 0);
                }
                i++;
            }
            return pm;
        }
        
        public bool RemoveCustomRibbonButton(string tabName, string panelName, string text)
        {
            RibbonTabSource tab = null;
            RibbonPanelSource panel = null;
            RibbonRow row = null;
            if (!GetPanel(false, tabName, panelName, ref tab, ref panel, ref row))
                return false;

            if (tab == null || panel == null || row == null)
                return false;

            RibbonItem tempItem = FindFirst<RibbonItem>("Text", text, row.Items);
            if (tempItem != null)
            {
                row.Items.Remove(tempItem);
                if (row.Items.Count == 0)
                    Cs.MenuGroup.RibbonRoot.RemovePanel(panel);
                if (tab.Items.Count == 0)
                {
                    Cs.MenuGroup.RibbonRoot.RibbonTabSources.Remove(tab);
                    foreach (Workspace ws in Cs.Workspaces)
                    {
                        Cs.RemoveTabFromWorkspace(ws, tab);
                        ws.SetIsModified();
                    }
                }
            }
            Cs.MenuGroup.SetModifiedItems(MenuGroup.ItemsCollection.RibbonRoot);
            return true;
        }

        public bool AddCustomRibbonButton(string tabName, string panelName, string text, 
            string macroName)
        {
            MenuMacro macro = FindFirst<MenuMacro>("macro.Name", macroName, MacroGroup.MenuMacros);
            if (macro == null)
                return false;

            RibbonTabSource tab = null;
            RibbonPanelSource panel = null;
            RibbonRow row = null;
            if (!GetPanel(true, tabName, panelName, ref tab, ref panel, ref row))
                return false;

            if (tab == null || panel == null || row == null)
                return false;

            RibbonItem tempItem = FindFirst<RibbonItem>("Text", text, row.Items);
            if (tempItem != null)
                return false;

            RibbonCommandButton btn = new RibbonCommandButton(panel);
            btn.Text = text;
            btn.MacroID = macro.ElementID;
            btn.ButtonStyle = RibbonButtonStyle.LargeWithText;
            row.Items.Add(btn);

            foreach (Workspace ws in Cs.Workspaces)
            {
                WSRibbonTabSourceReference temp = FindFirst<WSRibbonTabSourceReference>(
                    "TabId", tab.ElementID, ws.WorkspaceRibbonRoot.WorkspaceTabs);
                if (temp != null)
                    continue;
                WSRibbonTabSourceReference wsTab = new WSRibbonTabSourceReference(
                    ws.WorkspaceRibbonRoot);
                wsTab.MenuGroup = Cs.MenuGroup.Name;
                wsTab.TabId = tab.ElementID;
                wsTab.Show = true;
                ws.WorkspaceRibbonRoot.WorkspaceTabs.Add(wsTab);
                ws.SetIsModified();
            }

            Cs.MenuGroup.SetModifiedItems(MenuGroup.ItemsCollection.RibbonRoot);
            return true;
        }

        public CustomizationSection Cs
        {
            get
            {
                if (mCs == null)
                {
                    if (File.Exists(CuiFileWithExt))
                        mCs = new CustomizationSection(CuiFileWithExt);
                    else
                        mCs = new CustomizationSection();
                }
                if (mCs.Workspaces.Count == 0)
                {
                    Workspace ws = new Workspace(mCs, s_newWorkspace);
                    mCs.Workspaces.SetDefaultWorkspace(ws.ElementID);
                }
                return mCs;
            }
        }

        public void ShowCommandLineWindow(bool show)
        {
            Workspace ws = FindFirst<Workspace>("Name", s_newWorkspace, Cs.Workspaces);
            if (ws != null)
            {
                WorkspaceDockableWindow d = ws.DockableWindows.FindDockableWindow("Command Line");
                if (d == null)
                    d = ws.DockableWindows.FindDockableWindow("命令行");
                if (d == null)
                    return;
                d.Display = show ? YesNoIgnoreToggle.yes : YesNoIgnoreToggle.no;
                ws.LayoutModelTabs = OnOffIgnoreToggle.off;
            }
        }

        protected Cui() { }

        public string CuiFile
        {
            get
            {
                if (this == Cui.Current)
                    return Cui.CurCuiFile;
                else
                    return CuiDir + "\\" + mCuiFile;
            }
        }

        private string CuiFileWithExt
        {
            get
            {
                return CuiFile + s_cuix;
            }
        }

        private MacroGroup MacroGroup
        {
            get
            {
                if (Cs.MenuGroup.MacroGroups.Count == 0)
                {
                    new MacroGroup(s_newMacroGroup, Cs.MenuGroup);
                    Cs.MenuGroupName = s_newMenuGroup;
                }
                return Cs.MenuGroup.MacroGroups[0];
            }
        }

        private static void Reload(string unloadedMenuGroup, string loadFile, bool showRibbon)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            short oldFileDiaValue = (short)AcadApp.GetSystemVariable("FILEDIA");
            doc.SendStringToExecute(string.Format(@"FILEDIA 0 "), false, false, true);
            doc.SendStringToExecute(string.Format(@"CUIUNLOAD ""{0}"" ", unloadedMenuGroup),
                false, false, true);
            doc.SendStringToExecute(string.Format("MENU\n\"{0}\"\n", loadFile), 
                false, false, true);
            doc.SendStringToExecute(string.Format("FILEDIA\n{0}\n", oldFileDiaValue), 
                false, false, true);
            doc.SendStringToExecute(showRibbon ? "Ribbon " : "RibbonClose ", false, false, true);
            s_curCui = null; // invalid the current cui
            s_curCs = null;
        }

        private bool GetPanel(bool createIfNotExisting, string tabName, string panelName,
            ref RibbonTabSource tab, ref RibbonPanelSource panel, ref RibbonRow row)
        {
            RibbonTabSourceCollection tabs = Cs.MenuGroup.RibbonRoot.RibbonTabSources;
            tab = FindFirst<RibbonTabSource>("Name", tabName, tabs);
            if (tab == null)
            {
                if (!createIfNotExisting)
                    return false;
                tab = new RibbonTabSource(Cs.MenuGroup.RibbonRoot);
                tab.Name = tabName;
                tab.Text = tabName;
                tabs.Add(tab);
            }
            RibbonPanelSourceCollection panels = Cs.MenuGroup.RibbonRoot.RibbonPanelSources;
            panel = FindFirst<RibbonPanelSource>("Name", panelName, panels);
            RibbonPanelSourceReference panelRef = null;
            if (panel != null)
            {
                panelRef = FindFirst<RibbonPanelSourceReference>("PanelId", panel.ElementID, 
                    tab.Items);
            }
            else
            {
                if (!createIfNotExisting)
                    return false;
                panel = new RibbonPanelSource(Cs.MenuGroup.RibbonRoot);
                panel.Name = panelName;
                panel.Text = panelName;
                panels.Add(panel);
            }
            if (panelRef == null)
            {
                if (!createIfNotExisting)
                    return false;
                panelRef = new RibbonPanelSourceReference(tab);
                panelRef.PanelId = panel.ElementID;
                tab.Items.Add(panelRef);
            }
            if (panel.Items.Count == 0)
            {
                if (!createIfNotExisting)
                    return false;
                panel.Items.Add(new RibbonRow(panel));
            }

            row = panel.Items[panel.Items.Count - 1] as RibbonRow;
            return !(row == null);
        }

        public static string CurCuiFile
        {
            get
            {
                return (string)AcadApp.GetSystemVariable("MENUNAME");
            }
        }

        static string CuiDir
        {
            get
            {
                if (string.IsNullOrEmpty(s_cuiDir))
                    s_cuiDir = Path.GetDirectoryName(CurCuiFile);
                return s_cuiDir;
            }
        }

        static CustomizationSection CurCs
        {
            get
            {
                if (s_curCs == null)
                {
                    if (File.Exists(CurCuiFile + s_cuix))
                        s_curCs = new CustomizationSection(CurCuiFile + s_cuix);
                }
                return s_curCs;
            }
        }

        static bool CurIsAcad
        {
            get
            {
                return (CurCuiFile.ToLower() == AcadCuiFile.ToLower());
            }
        }

        public static string AcadCuiFile
        {
            get
            {
                return CuiDir + "\\acad";
            }
        }

        static List<T> Find<T>(string keyPropertyName, StringCollection keys, IList list)
        {
            if (string.IsNullOrEmpty(keyPropertyName) || keys.Count == 0
                || list == null || list.Count == 0)
                return null;
            List<T> tempList = new List<T>();
            foreach (T element in list)
            {
                string[] props = keyPropertyName.Split('.');
                object o = element;
                foreach (string prop in props)
                {
                    if (o == null)
                        break;
                    PropertyInfo pi = o.GetType().GetProperty(prop); // causes performance issues?
                    if (pi == null)
                    {
                        FieldInfo fi = o.GetType().GetField(prop);
                        if (fi == null)
                            continue;
                        o = fi.GetValue(o);
                    }
                    else
                        o = pi.GetValue(o, null);
                }
                if (o is string)
                {
                    if (string.IsNullOrEmpty(o as string))
                        continue;
                    if (keys.Contains((string)o))
                        tempList.Add(element);
                }
            }
            return tempList;
        }

        static T FindFirst<T>(string keyPropertyName, string key, IList list)
        {
            List<T> tempList = Find<T>(keyPropertyName, new StringCollection() { key }, list);
            if (tempList != null && tempList.Count > 0)
                return tempList[0];
            else
                return default(T);
        }

        static bool Remove<T>(string keyPropertyName, StringCollection keys, IList list)
        {
            List<T> tempList = Find<T>(keyPropertyName, keys, list);
            if (tempList == null || tempList.Count == 0)
                return false;
            return Remove<T>(list, tempList);
        }

        static bool Remove<T>(IList list, List<T> tempList)
        {
            foreach (T element in tempList)
                list.Remove(element);
            tempList.Clear();
            return true;
        }

        static string s_cuix = ".cuix";
        static string s_newWorkspace = "MyWorkspace";
        static string s_newMacroGroup = "MyMacroGroup";
        static string s_newMenuGroup = "MyMenuGroup";
        static string s_cuiDir;

        static CustomizationSection s_curCs;
        static Cui s_curCui;

        CustomizationSection mCs;
        string mCuiFile;
    }

    class MacroData
    {
        public MacroData(string name, string cmd, string largeImage, string smallImage)
        {
            Name = name;
            Command = cmd;
            LargeImage = largeImage;
            SmallImage = smallImage;
        }
        public string Name { get; set; }
        public string Command { get; set; }
        public string LargeImage { get; set; }
        public string SmallImage { get; set; }
    }

    class MenuData
    {
        public string Name { get; set; }
        public string MacroName { get; set; }
        public List<MenuData> Items
        {
            get
            {
                if (mItems == null)
                    mItems = new List<MenuData>();
                return mItems;
            }
        }
        public string Id
        {
            get
            {
                if (mId == null)
                    mId = Name;
                return mId;
            }
            set
            {
                mId = value;
            }
        }
        private string mId;
        private List<MenuData> mItems;
    }

    class CuiTest
    {
        [CommandMethod("AddMenus")]
        public void AddMenus()
        {
            Cui.Current.AddMenuMacros(CreateMacroData());
            Cui.Current.AddMenu(CreateMenuData());
            Cui.Current.Save();
            Cui.Current.Load(true);
        }

        [CommandMethod("RemoveMenus")]
        public void RemoveMenus()
        {
            Cui.Current.RemoveMenuMacros(new StringCollection() 
                { "CmdName0", "CmdName1", "CmdName2", "CmdName3" });
            Cui.Current.RemoveMenu(CreateMenuData());
            Cui.Current.Save();
            Cui.Current.Load(true);
        }

        [CommandMethod("NewCui")]
        public void NewCui()
        {
            Cui c = new Cui("My");
            c.AddMenuMacros(CreateMacroData());
            c.AddMenu(CreateMenuData());
            for (int i = 0; i < 10; i++)
                c.AddCustomRibbonButton("tab2", "panel2",
                    "hahaha" + i, "CmdName" + (i % 4));
            for (int i = 0; i < 15; i++)
                c.AddCustomRibbonButton("tab2", "panel3",
                    "hehehe" + i, "CmdName" + (i % 4));
            c.Save();
            c.Load(true);
        }

        [CommandMethod("RemoveCurrentMyCui")]
        public void RemoveCurrentMyCui()
        {
            Cui.Current.RemoveMenuMacros(new StringCollection() 
                { "CmdName0", "CmdName1", "CmdName2", "CmdName3" });
            Cui.Current.RemoveMenu(CreateMenuData());
            for (int i = 0; i < 10; i++)
                Cui.Current.RemoveCustomRibbonButton("tab2", "panel2", "hahaha" + i);
            for (int i = 0; i < 15; i++)
                Cui.Current.RemoveCustomRibbonButton("tab2", "panel3", "hehehe" + i);
            Cui.Current.Save();
            Cui.Current.Load(true);
        }

        [CommandMethod("RestoreToAcadCui")]
        public void RestoreToAcadCui()
        {
            Cui.RestoreToAcadCui();
        }

        private List<MacroData> CreateMacroData()
        {
            List<MacroData> mmc = new List<MacroData>();
            for (int i = 0; i < 4; i++)
                mmc.Add(new MacroData("CmdName" + i,
                    string.Format("^^_CIRCLE 0,0 {0} ", (i + 1)),
                    "RCDATA_32_OSNFRO",
                    "RCDATA_16_OSNFRO"));
            return mmc;
        }

        private MenuData CreateMenuData()
        {
            MenuData menu = new MenuData();
            for (int i = 0; i < 10; i++)
            {
                MenuData sub = new MenuData();
                sub.Name = "Menu" + i;
                for (int j = 0; j < 4; j++)
                {
                    MenuData leaf = new MenuData();
                    leaf.Name = "MenuItem" + i + j;
                    leaf.MacroName = "CmdName" + j % 4;
                    sub.Items.Add(leaf);
                }
                menu.Items.Add(sub);
            }
            return menu;
        }
    }

    static class TransHelper
    {
        public static TResult TransResult<TResult>(Transaction tr, Func<Transaction, TResult> func)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                TResult res = func(tr);
                if (newTr != null)
                    newTr.Commit();
                return res;
            }
        }

        public static TResult TransResult<T, TResult>(Transaction tr, Func<Transaction, T, TResult>
            func, T t)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                TResult res = func(tr, t);
                if (newTr != null)
                    newTr.Commit();
                return res;
            }
        }

        public static TResult TransResult<T1, T2, TResult>(Transaction tr, Func<Transaction, T1, T2,
            TResult> func, T1 t1, T2 t2)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                TResult res = func(tr, t1, t2);
                if (newTr != null)
                    newTr.Commit();
                return res;
            }
        }

        public static TResult TransResult<T1, T2, T3, TResult>(Transaction tr, Func<Transaction, 
            T1, T2, T3, TResult> func, T1 t1, T2 t2, T3 t3)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                TResult res = func(tr, t1, t2, t3);
                if (newTr != null)
                    newTr.Commit();
                return res;
            }
        }

        public static void Trans(Transaction tr, Action<Transaction> action)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                action(tr);
                if (newTr != null)
                    newTr.Commit();
            }
        }

        public static void Trans<T>(Transaction tr, Action<Transaction, T> action, T t)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                action(tr, t);
                if (newTr != null)
                    newTr.Commit();
            }
        }

        public static void Trans<T1, T2>(Transaction tr, Action<Transaction, T1, T2> action,
            T1 t1, T2 t2)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                action(tr, t1, t2);
                if (newTr != null)
                    newTr.Commit();
            }
        }

        public static void Trans<T1, T2, T3>(Transaction tr, Action<Transaction, T1, T2, T3> action,
            T1 t1, T2 t2, T3 t3)
        {
            Transaction newTr = null;
            if (tr == null)
            {
                newTr = G.Db.TransactionManager.StartTransaction();
                tr = newTr;
            }
            using (newTr)
            {
                action(tr, t1, t2, t3);
                if (newTr != null)
                    newTr.Commit();
            }
        }

    }

    class App : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                AcadApp.DocumentManager.DocumentActivated += new DocumentCollectionEventHandler(
                    DocumentManager_DocumentActivated);
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("App_Initialize failed by {0}", ex.Message));
            }
        }
        public void Terminate()
        {
            try
            {
                Cui.ClearWsProfile(); // this is a workaround because Cui.RestoreCui doesn't work.
            }
            catch (System.Exception ex)
            {
                Debug.Fail(string.Format("App_Terminate failed by {0}", ex.Message));
            }
        }
        void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            G.InvalidDocument();
        }
    }

    static class G
    {
        public static int Rand(int i)
        {
            if (sRand == null)
                sRand = new Random();
            return Math.Abs(sRand.Next()) % i;
        }

        public static Document Doc
        {
            get
            {
                if (sDoc == null)
                    sDoc = AcadApp.DocumentManager.MdiActiveDocument;
                return sDoc;
            }
        }

        public static Editor Ed
        {
            get
            {
                if (sEd == null && Doc != null)
                    sEd = Doc.Editor;
                return sEd;
            }
        }

        public static Database Db
        {
            get
            {
                if (sDb == null && Doc != null)
                    sDb = Doc.Database;
                return sDb;
            }
        }

        public static void InvalidDocument()
        {
            sDoc = null;
            sEd = null;
            sDb = null;
        }

        public static void Log(string msg)
        {
            Ed.WriteMessage(msg);
        }

        public static bool SaveStringToDwg(string key, string value, Transaction tr)
        {
            return TransHelper.TransResult<string, string, bool>(tr, pTransSaveStringToDwg,
                key, value);
        }

        private static bool pTransSaveStringToDwg(Transaction tr, string key, string value)
        {
            DBDictionary dict = tr.GetObject(pGetDict(true, tr), OpenMode.ForWrite)
                as DBDictionary;
            if (dict == null)
                return false;
            Xrecord xrec;
            bool newXrec = false;
            if (dict.Contains(key))
            {
                DBObject obj = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
                xrec = obj as Xrecord;
                if (xrec == null)
                {
                    obj.Erase();
                    xrec = new Xrecord();
                    newXrec = true;
                }
            }
            else
            {
                xrec = new Xrecord();
                newXrec = true;
            }
            xrec.XlateReferences = true;
            xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, value));
            if (newXrec)
            {
                dict.SetAt(key, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }
            return true;
        }

        public static string LoadStringFromDwg(string key, Transaction tr)
        {
            return TransHelper.TransResult<string, string>(tr, pTransLoadStringFromDwg, key);
        }

        private static string pTransLoadStringFromDwg(Transaction tr, string key)
        {
            ObjectId dictId = pGetDict(false, tr);
            if (dictId.IsNull)
                return null;

            DBDictionary dict = tr.GetObject(dictId, OpenMode.ForRead) as DBDictionary;
            if (dict == null)
                return null;
            if (dict.Contains(key))
            {
                DBObject obj = tr.GetObject(dict.GetAt(key), OpenMode.ForRead);
                Xrecord xrec = obj as Xrecord;
                if (xrec == null)
                    return null;
                ResultBuffer rb = xrec.Data;
                foreach (TypedValue tv in rb)
                {
                    return tv.Value as string;
                }
            }
            return null;
        }

        public static bool AlmostZero(double d)
        {
            return Equal(d, 0);
        }

        public static bool Equal(double a, double b)
        {
            return (Math.Abs(a - b) < 1E-6);
        }

        public static double Degree2Radian(double degree)
        {
            return degree * Math.PI / 180;
        }

        public static string DllPath
        {
            get
            {
                if (string.IsNullOrEmpty(sDllPath))
                {
                    sDllPath = Assembly.GetAssembly(typeof(G)).Location;
                    sDllPath = Path.GetDirectoryName(sDllPath);
                }
                return sDllPath;
            }
        }

        public static string GetFullFileName(string file)
        {
            return Path.Combine(G.DllPath, file);
        }

        private static ObjectId pGetDict(bool createIfNotExisting, Transaction tr)
        {
            return TransHelper.TransResult<bool, ObjectId>(tr, pTransGetDict, createIfNotExisting);
        }

        private static ObjectId pTransGetDict(Transaction tr, bool createIfNotExisting)
        {
            ObjectId dictId;
            DBDictionary namedObjects = (DBDictionary)tr.GetObject(Db.NamedObjectsDictionaryId,
                OpenMode.ForRead);
            if (!namedObjects.Contains(AppKey))
            {
                if (!createIfNotExisting)
                    return ObjectId.Null;
                DBDictionary dict = new DBDictionary();
                namedObjects.UpgradeOpen();
                dictId = namedObjects.SetAt(AppKey, dict);
                tr.AddNewlyCreatedDBObject(dict, true);
            }
            else
            {
                dictId = namedObjects.GetAt(AppKey);
            }
            return dictId;
        }

        private static Document sDoc;
        private static Editor sEd;
        private static Database sDb;
        private static string sAppKey = "MyApp";
        private static string AppKey { get { return sAppKey; } }
        private static string sDllPath;
        private static Random sRand;
    }
}
