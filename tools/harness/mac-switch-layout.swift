// Switch this Mac to the next enabled keyboard layout, like the input source hotkey does (tests on macOS).
//   swiftc -O tools/harness/mac-switch-layout.swift -o .harness/mac-switch-layout && .harness/mac-switch-layout
import Carbon

func sourceID(_ s: TISInputSource) -> String {
    guard let p = TISGetInputSourceProperty(s, kTISPropertyInputSourceID) else { return "?" }
    return Unmanaged<CFString>.fromOpaque(p).takeUnretainedValue() as String
}

let current = TISCopyCurrentKeyboardInputSource().takeRetainedValue()
let filter = [
    kTISPropertyInputSourceCategory as String: kTISCategoryKeyboardInputSource as String,
    kTISPropertyInputSourceIsSelectCapable as String: true,
] as CFDictionary
let layouts = (TISCreateInputSourceList(filter, false).takeRetainedValue() as! [TISInputSource])
    .filter { sourceID($0).hasPrefix("com.apple.keylayout.") }
guard let i = layouts.firstIndex(where: { sourceID($0) == sourceID(current) }), layouts.count > 1 else {
    print("nothing to switch to: \(layouts.map(sourceID))")
    exit(1)
}
let next = layouts[(i + 1) % layouts.count]
let status = TISSelectInputSource(next)
print("\(sourceID(current)) -> \(sourceID(next)) (status \(status))")
