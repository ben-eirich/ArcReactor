# ArcReactor

This project is primarily me recording my efforts to learning the fundamentals of data compression by implementing 
the core, common algorithms. In the process, I hope to provide very minimalist, clear and concise example source code
for common algorithms.

This project is NOT a cool new compression algorithm. It's not better or faster than anything else. It's just educational.

I currently implement the following algorithms:

- A byte-based non-entropy-coded compressor.
- A bit-oriented non-entropy coded compressor. 
- A standard LZ+Huffman large-window compressor. 
- A minimal LZ+Arithmetic coded compressor with context modelling.

One of the goals of all of these implementations is to have a concise, "distraction-free" implementation of the core
idea of each algorithm. As such, common optimizations such as previous match references are not implemented at this time.

The primary TODO list is:
- Create a minimal ROLZ-based algorithm.
- Implement binary-tree based match finding.
- Write an optimal parser.
- Try my hand at a more agressive arithmetic coder, possibly integrating standard LZ with ROLZ, and probably 
  utilizing the other common tricks like rep matches or optimal parse.
- Write a command-line based zip-style archiver interface on top of these algorithms.

To give an idea of the relative performance of these algorithms, as they are currently implemented, at "Max" 
compression settings, here are some benchmarks for my test corpus, which is 15 files totalling 311,956,892
uncompressed bytes.

- Deflate: 104,089,206
- Bytewise compressor: 103,967,229
- Bitwise compressor: 96,901,942
- LZ+Huffman compressor: 90,780,029
- LZ+Arithmetic compressor: 84,206,745

The compression benchmark I use is the silesia corpus, plus enwik8, and in addition I added two "small files" 
to excercise the small file use case. The two small files are a `README.md` (from sqlite3), and `lua.c`.