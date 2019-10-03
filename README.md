# ArcReactor

This project is primarily me recording my efforts to learning the fundamentals of data compression by implementing 
the core, common algorithms. In the process, I hope to provide very minimalist, clear and concise example source code
for common algorithms.

This project is NOT a cool new compression algorithm. It's not better or faster than anything else. It's just educational.

I currently implement the following algorithms:

- A byte-based non-entropy-coded compressor.
- A bit-oriented non-entropy coded compressor. 
- A standard LZ+Huffman large-window compressor. 

One of the goals of all of these implementations is to have a concise, "distraction-free" implementation of the core
idea of each algorithm. As such, common optimizations such as previous match references are not implemented at this time.

The primary TODO list is:
- Create a minimal LZ+Arithmetic coding algorithm.
- Create a minimal ROLZ-based algorithm.
- Implement binary-tree based match finding.
- Write an optimal parser.
- Try my hand at a more agressive arithmetic coder, possibly integrating standard LZ with ROLZ, and probably 
  utilizing the other common tricks like rep matches or optimal parse.
- Write a command-line based zip-style archiver interface on top of these algorithms.
