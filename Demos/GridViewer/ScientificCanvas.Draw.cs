﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using CSharpGL;

namespace GridViewer
{
    public partial class ScientificCanvas
    {

        void ScientificCanvas_OpenGLDraw(object sender, PaintEventArgs e)
        {
            vec4 clearColor = this.Scene.ClearColor.ToVec4();
            OpenGL.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);

            Point mousePosition = this.PointToClient(Control.MousePosition);
            this.Scene.Render(RenderModes.Render, this.ClientRectangle, mousePosition);
        }
    }
}
