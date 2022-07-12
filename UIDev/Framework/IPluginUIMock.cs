using System;

using ImGuiScene;

namespace UIDev {
  interface IPluginUIMock : IDisposable {
    void Initialize(SimpleImGuiScene scene);
  }
}
