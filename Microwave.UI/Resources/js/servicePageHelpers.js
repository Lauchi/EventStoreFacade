const initMapNodes = (myNodes, myEdges) => {
const g = {
    "nodes": myNodes,
    "edges": myEdges
};

sigma.classes.graph.addMethod('adjacentEdges', function(id) {
    var a = this.allNeighborsIndex[id],
        eid,
        target,
        edges = [];
    for(target in a) {
        for(eid in a[target]) {
            edges.push(a[target][eid]);
        }
    }
    return edges;
});

const s = new sigma({
    graph: g,
    renderer: {
        container: document.getElementById('container'),
        type: 'canvas'
    },
    settings: {
        doubleClickEnabled: false,
        minEdgeSize: 3,
        maxEdgeSize: 4,
        defaultEdgeHoverColor: '#ccc',
        defaultNodeColor: '#343a40',
        edgeHoverSizeRatio: 1,
        edgeHoverExtremities: true,
        defaultEdgeType: 'arrow',
    }
});

s.bind('clickNode', function(e) {
    location.href = e.data.node.serviceAddress + 'MicrowaveDashboard';
});


s.bind('overNode',
    function(e)
    {
        const nodeId = e.data.node.id;
        const adjacentEdges = s.graph.adjacentEdges(nodeId);
        adjacentEdges.forEach(
            function (edge) {
                const f1 = adjacentEdges.filter(function(e) { return e.target === edge.source && e.source === edge
                    .target});
                if (f1.length === 1) {
                    edge.color = '#28a745';
                }

                if (edge.source === nodeId){
                    edge.color = '#28a745';
                }
            }
        );
        s.refresh();
    });

s.bind('outNode',
    function(e)
    {
        const nodeId = e.data.node.id;
        s.graph.adjacentEdges(nodeId).forEach(
            function (ee) {
                ee.color = '#ccc';
            }
        );
        s.refresh();
    });
};

export { initMapNodes };