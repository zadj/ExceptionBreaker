﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace ExceptionBreak.Implementation {
    public class DebugSessionWatcher : IDebugEventCallback2, IDisposable {
        public event EventHandler DebugSessionChanged = delegate {};

        private readonly IVsDebugger debugger;
        private readonly IDiagnosticLogger logger;

        private IDebugSession2 debugSession;

        public DebugSessionWatcher(IVsDebugger debugger, IDiagnosticLogger logger) {
            this.debugger = debugger;
            this.logger = logger;

            var hr = this.debugger.AdviseDebugEventCallback(this);
            if (hr != VSConstants.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }

        public IDebugSession2 DebugSession {
            get { return this.debugSession; }
            private set {
                if (value == this.debugSession)
                    return;
                
                this.logger.WriteLine("DebugSession: Changing to {0}.", this.ToComPtrString(value));
                this.debugSession = value;
                this.DebugSessionChanged(this, EventArgs.Empty);
                this.logger.WriteLine("DebugSession: Changed to {0}.", this.ToComPtrString(value));
            }
        }
        

        private int ProcessSessionCreateOrAttachEvent(IDebugSessionEvent2 @event, string logSessionAs) {
            IDebugSession2 session;
            this.logger.WriteLine("Event: Debug session {0}.", logSessionAs);

            var hr = @event.GetSession(out session);
            if (hr != VSConstants.S_OK)
                Marshal.ThrowExceptionForHR(hr);
            
            this.DebugSession = session;
            return VSConstants.S_OK;
        }

        private int ProcessSessionDestroyEvent() {
            this.logger.WriteLine("Event: Debug session destroyed.");
            this.DebugSession = null;
            return VSConstants.S_OK;
        }

        //private int ProcessProgramCreateEvent(IDebugProgram2 program) {
        //    this.logger.WriteLine("Event: Debug program {0} ({1}) created.", this.GetProgramName(program), this.ToComPtrString(program));
        //    this.DebugPrograms.Add(program);
        //    this.logger.WriteLine("DebugPrograms: Count = {0}.", this.DebugPrograms.Count);
        //    return VSConstants.S_OK;
        //}

        //private int ProcessProgramDestroyEvent(IDebugProgram2 program) {
        //    this.logger.WriteLine("Event: Debug program {0} ({1}) destroyed.", this.GetProgramName(program), this.ToComPtrString(program));
        //    this.DebugPrograms.Remove(program);
        //    this.logger.WriteLine("DebugPrograms: Count = {0}.", this.DebugPrograms.Count);
        //    return VSConstants.S_OK;
        //}

        int IDebugEventCallback2.Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib) {
            this.logger.WriteLine("Debug event: pEngine = {0}, pProcess = {1}, pProgram = {2}, pThread = {3}, pEvent = {4}, riidEvent = {5}, dwAttrib = {6}",
                                  pEngine, pProcess, pProgram, pThread, pEvent, riidEvent, dwAttrib);
            try {
                if (riidEvent == typeof(IDebugSessionCreateEvent2).GUID)
                    return this.ProcessSessionCreateOrAttachEvent((IDebugSessionEvent2)pEvent, "created");

                if (riidEvent == typeof(IDebugAttachCompleteEvent2).GUID)
                    return this.ProcessSessionCreateOrAttachEvent((IDebugSessionEvent2)pEvent, "attached");

                if (riidEvent == typeof(IDebugSessionDestroyEvent2).GUID)
                    return this.ProcessSessionDestroyEvent();

                //if (riidEvent == typeof(IDebugProgramCreateEvent2).GUID)
                //    return this.ProcessProgramCreateEvent(pProgram);

                //if (riidEvent == typeof(IDebugProgramDestroyEvent2).GUID)
                //    return this.ProcessProgramDestroyEvent(pProgram);
            }
            catch (Exception ex) {
                this.logger.WriteLine("Unexpected exception: " + ex);
            }

            return VSConstants.S_OK;
        }

        //private string GetProgramName(IDebugProgram2 program) {
        //    IDebugProcess2 process;
        //    var hr = program.GetProcess(out process);
        //    if (hr != VSConstants.S_OK)
        //        Marshal.ThrowExceptionForHR(hr);

        //    string processName;
        //    hr = process.GetName((uint)enum_GETNAME_TYPE.GN_NAME, out processName);
        //    if (hr != VSConstants.S_OK)
        //        Marshal.ThrowExceptionForHR(hr);

        //    string programName;
        //    hr = program.GetName(out programName);
        //    if (hr != VSConstants.S_OK)
        //        Marshal.ThrowExceptionForHR(hr);

        //    return "'" + processName + "' (" + programName + ")";
        //}

        private string ToComPtrString(object comObject) {
            if (comObject == null)
                return "null";

            return Marshal.GetIUnknownForObject(comObject).ToString();
        }

        public void Dispose() {
            this.debugger.UnadviseDebugEventCallback(this);
        }
    }
}