using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;


namespace StarterKit
{
    class KDNode
    {
        public bool photon;
        public Vector3 point;
        public KDNode left;
        public KDNode right;
        public KDNode parent;
        public bool visited = false;
        public int k;
        public int leftk;
        public int rightk;
        public int parentk;
        

        public KDNode()
        {
        }

        public KDNode(KDNode node)
        {
            photon = node.photon;
            point = node.point;
            left = node.left;
            right = node.right;
            parent = node.parent;
            visited = node.visited;
            k = node.k;
        }

        public void Copy(KDNode node)
        {
            photon = node.photon;
            point = node.point;
            left = node.left;
            right = node.right;
            parent = node.parent;
            visited = node.visited;
            k = node.k;
        }



    }
}
